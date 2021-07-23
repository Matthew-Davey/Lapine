namespace Lapine {
    using System;
    using System.Linq;
    using Lapine.Client;
    using Bogus;
    using FluentAssertions;
    using Xbehave;

    public class ChannelTests : Faker {
        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
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
            "Then the broker should report an open channel".x(async () => {
                var channels = await broker.GetChannels().ToListAsync();
                channels.Should().HaveCount(1);
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void OpenMultiple(String brokerVersion, BrokerProxy broker, AmqpClient subject) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When a channel is opened".x(async () => {
                for (var i = 0; i < 10; i++)
                    await subject.OpenChannelAsync();
            });
            "Then the broker should report an open channel".x(async () => {
                var channels = await broker.GetChannels().ToListAsync();
                channels.Should().HaveCount(10);
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
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
            "Then the broker should report no open channels".x(async () => {
                var channels = await broker.GetChannels().ToListAsync();
                channels.Should().BeEmpty();
            });
        }
    }
}
