namespace Lapine.Client {
    using System;
    using System.Linq;
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
                var exchanges = await broker.GetExchangesAsync().ToListAsync();

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
                var exchanges = await broker.GetExchangesAsync().ToListAsync();

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
                var exchanges = await broker.GetExchangesAsync().ToListAsync();

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
                var exchanges = await broker.GetExchangesAsync().ToListAsync();

                exchanges.Should().Contain(exchangeDefinition);
            });
        }

        [Scenario]
        // This test requires management enabled containers due to the use of rabbitmqadmin to declare exchanges...
        [Example("3.9-rc-management-alpine")]
        [Example("3.8-management-alpine")]
        [Example("3.7-management-alpine")]
        public void RedeclareExchanges(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has an exchange declared".x(async () => {
                exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word());
                await broker.ExchangeDeclareAsync(exchangeDefinition);
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to redeclare the exchange".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeclareExchangeAsync(exchangeDefinition);
                });
            });
            "Then no exception is thrown".x(() => {
                exception.Should().BeNull();
            });
        }

        [Scenario]
        // This test requires management enabled containers due to the use of rabbitmqadmin to declare exchanges...
        [Example("3.9-rc-management-alpine")]
        [Example("3.8-management-alpine")]
        [Example("3.7-management-alpine")]
        public void RedeclareExchangeWithDifferentParameters(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has an exchange declared".x(async () => {
                exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word());
                await broker.ExchangeDeclareAsync(exchangeDefinition);
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
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

        [Scenario]
        // This test requires management enabled containers due to the use of rabbitmqadmin to declare exchanges...
        [Example("3.9-rc-management-alpine")]
        [Example("3.8-management-alpine")]
        [Example("3.7-management-alpine")]
        public void DeleteExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has an exchange declared".x(async () => {
                exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word());
                await broker.ExchangeDeclareAsync(exchangeDefinition);
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client deletes the exchange".x(async () => {
                await channel.DeleteExchangeAsync(exchangeDefinition.Name);
            });
            "Then the exchange should no longer exist on the broker".x(async () => {
                var exchanges = await broker.GetExchangesAsync().ToListAsync();
                exchanges.Should().NotContain(exchangeDefinition);
            });
        }
    }
}
