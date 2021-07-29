namespace Lapine.Client {
    using System;
    using System.Linq;
    using Bogus;
    using FluentAssertions;
    using Xbehave;
    using Xunit;

    public class QueueTests : Faker {
        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void DeclareClassicQueue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client declares a classic queue".x(async () => {
                await channel.DeclareQueueAsync(queueDefition = QueueDefinition.Create(Lorem.Word()) with {
                    AutoDelete = true,
                    Exclusive  = true,
                    Durability = Durability.Transient
                });
            });
            "Then the queue is created on the broker".x(async () => {
                var queues = await broker.GetQueuesAsync().ToListAsync();

                queues.Should().Contain(queueDefition);
            });
        }

        [Scenario]
        // This test requires management enabled containers due to the use of rabbitmqadmin to declare queues...
        [Example("3.9-management")]
        [Example("3.8-management")]
        [Example("3.7-management")]
        public void RedeclareQueue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                queueDefinition = QueueDefinition.Create(Lorem.Word()) with {
                    Durability = Durability.Durable,
                };
                await broker.QueueDeclareAsync(queueDefinition);
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to redeclare the queue".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeclareQueueAsync(queueDefinition);
                });
            });
            "Then no exception is thrown".x(() => {
                exception.Should().BeNull();
            });
        }

        [Scenario]
        // This test requires management enabled containers due to the use of rabbitmqadmin to declare queues...
        [Example("3.9-management")]
        [Example("3.8-management")]
        [Example("3.7-management")]
        public void RedeclareQueueWithDifferentParameters(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                queueDefinition = QueueDefinition.Create(Lorem.Word()) with {
                    Durability = Durability.Durable,
                };
                await broker.QueueDeclareAsync(queueDefinition);
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to redeclare the queue with a different parameter".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeclareQueueAsync(queueDefinition with {
                        Durability = Durability.Transient
                    });
                });
            });
            "Then a precondition failed exception is thrown".x(() => {
                exception.Should().NotBeNull();
                exception.Should().BeOfType(Type.GetType("Lapine.Client.PreconditionFailedException, Lapine.Core"));
            });
        }
    }
}
