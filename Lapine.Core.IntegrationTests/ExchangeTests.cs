namespace Lapine {
    using System;
    using System.Linq;
    using Lapine.Client;
    using Bogus;
    using FluentAssertions;
    using Xbehave;
    using Xunit;

    public class ExchangeTests : Faker {
        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DeclareDirectExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client declares a topic exchange".x(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Direct(Random.String2(12)));
            });
            "Then the exchange is created on the broker".x(async () => {
                var exchanges = await broker.GetExchanges().ToListAsync();

                exchanges.Should().Contain(exchangeDefinition);
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DeclareFanoutExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client declares a topic exchange".x(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Fanout(Random.String2(12)));
            });
            "Then the exchange is created on the broker".x(async () => {
                var exchanges = await broker.GetExchanges().ToListAsync();

                exchanges.Should().Contain(exchangeDefinition);
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DeclareHeadersExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client declares a topic exchange".x(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Headers(Random.String2(12)));
            });
            "Then the exchange is created on the broker".x(async () => {
                var exchanges = await broker.GetExchanges().ToListAsync();

                exchanges.Should().Contain(exchangeDefinition);
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DeclareTopicExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client declares a topic exchange".x(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Topic(Random.String2(12)));
            });
            "Then the exchange is created on the broker".x(async () => {
                var exchanges = await broker.GetExchanges().ToListAsync();

                exchanges.Should().Contain(exchangeDefinition);
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void RedeclareExchangeWithDifferentParameters(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "And the broker has an exchange declared".x(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Direct(Random.String2(8)));
            });
            "When the client attempts to redeclare the exchange with a different parameter".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeclareExchangeAsync(exchangeDefinition with {
                        Durability = Durability.Transient
                    });
                });
            });
            "Then a precondition failed exception is thrown".x(() => {
                exception.Should().NotBeNull();
                exception.Should().BeOfType(Type.GetType("Lapine.Client.PreconditionFailedException, Lapine.Core"));
            });
        }

        [Scenario]
        [Example("3.9-rc-alpine")]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DeclareExchangeWithReservedPrefix(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to declare an exchange with a reserved prefix".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeclareExchangeAsync(ExchangeDefinition.Direct("amq.notallowed"));
                });
            });
            "Then an access refused exception is thrown".x(() => {
                exception.Should().NotBeNull();
                exception.Should().BeOfType(Type.GetType("Lapine.Client.AccessRefusedException, Lapine.Core"));
            });
        }
    }
}
