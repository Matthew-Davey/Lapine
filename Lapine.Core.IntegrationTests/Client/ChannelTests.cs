namespace Lapine.Client {
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Bogus;
    using FluentAssertions;
    using Xbehave;

    public class ChannelTests : Faker {
        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void Open(String brokerVersion, BrokerProxy broker, AmqpClient subject) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When a channel is opened".x(async () => {
                await subject.OpenChannelAsync();
            });
            "Then the broker reports an open channel".x(async () => {
                var channels = await broker.GetChannelsAsync().ToListAsync();
                channels.Should().HaveCount(1);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void OpenMultiple(String brokerVersion, BrokerProxy broker, AmqpClient subject) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When 10 channels are opened".x(async () => {
                for (var i = 0; i < 10; i++)
                    await subject.OpenChannelAsync();
            });
            "Then the broker reports 10 open channels".x(async () => {
                var channels = await broker.GetChannelsAsync().ToListAsync();
                channels.Should().HaveCount(10);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void OpenMultipleConcurrently(String brokerVersion, BrokerProxy broker, AmqpClient subject) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When 10 channels are opened concurrently".x(async () => {
                await Task.WhenAll(
                    Enumerable.Range(0, 10)
                        .Select(_ => subject.OpenChannelAsync().AsTask())
                        .ToArray()
                );
            });
            "Then the broker reports 10 open channels".x(async () => {
                var channels = await broker.GetChannelsAsync().ToListAsync();
                channels.Should().HaveCount(10);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void Close(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the channel is closed".x(async () => {
                await channel.CloseAsync();
            });
            "Then the broker reports no open channels".x(async () => {
                var channels = await broker.GetChannelsAsync().ToListAsync();
                channels.Should().BeEmpty();
            });
        }
    }
}
