namespace Lapine.Client {
    using System;
    using System.Linq;
    using Bogus;
    using FluentAssertions;
    using Xbehave;
    using Xunit;

    using static System.Text.Encoding;

    public class QueueTests : Faker {
        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void DeclareClassicQueue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client declares a classic queue".x(async () => {
                await channel.DeclareQueueAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()) with {
                    AutoDelete = true,
                    Exclusive  = true,
                    Durability = Durability.Transient
                });
            });
            "Then the queue is created on the broker".x(async () => {
                var queues = await broker.GetQueuesAsync().ToListAsync();

                queues.Should().Contain(   queueDefinition);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void RedeclareQueue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
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
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void RedeclareQueueWithDifferentParameters(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
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

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void DeleteQueue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
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
            "When the client deletes the queue".x(async () => {
                await channel.DeleteQueueAsync(queueDefinition.Name);
            });
            "Then the queue no longer exists on the broker".x(async () => {
                var queues = await broker.GetQueuesAsync().ToListAsync();

                queues.Should().NotContain(queueDefinition);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void DeleteQueueConditionalNonEmpty(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, Exception exception) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                queueDefinition = QueueDefinition.Create(Lorem.Word());
                await broker.QueueDeclareAsync(queueDefinition);
            });
            "And the queue has some messages".x(async () => {
                await Enumerable.Repeat(0, 10)
                    .ToAsyncEnumerable()
                    .ForEachAsync(async _ => {
                        await broker.PublishMessage(
                            exchange  : "amq.default",
                            routingKey: queueDefinition.Name,
                            payload   : Lorem.Sentence()
                        );
                    });

                // Wait a few moments for the messages to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messages = await broker.GetMessageCount(queueDefinition.Name);
                    messages.Should().Be(10);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client deletes the queue, conditional on the queue being empty".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeleteQueueAsync(queueDefinition.Name, DeleteQueueCondition.Empty);
                });
            });
            "Then a precondition failed exception is thrown".x(() => {
                exception.Should().NotBeNull();
                exception.Should().BeOfType(Type.GetType("Lapine.Client.PreconditionFailedException, Lapine.Core"));
            });
            "And the queue still exists".x(async () => {
                var queues = await broker.GetQueuesAsync().ToListAsync();

                queues.Should().Contain(queueDefinition);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void PurgeQueue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            });
            "And the queue has some messages".x(async () => {
                await Enumerable.Repeat(0, 10)
                    .ToAsyncEnumerable()
                    .ForEachAsync(async _ => {
                        await broker.PublishMessage(
                            exchange  : "amq.default",
                            routingKey: queueDefinition.Name,
                            payload   : Lorem.Sentence()
                        );
                    });

                // Wait a few moments for the messages to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messages = await broker.GetMessageCount(queueDefinition.Name);
                    messages.Should().Be(10);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client purges the queue".x(async () => {
                await channel.PurgeQueueAsync(queueDefinition.Name);
            });
            "Then the queue has zero messages".x(async () => {
                var messages = await broker.GetMessageCount(queueDefinition.Name);
                messages.Should().Be(0);
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void GetMessageWithAutoAck(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, String payload, (DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)? message) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            });
            "And the queue has a message".x(async () => {
                await broker.PublishMessage(
                    exchange  : "amq.default",
                    routingKey: queueDefinition.Name,
                    payload   : payload = Lorem.Sentence()
                );

                // Wait a few moments for the message to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client retrieves the message with auto-ack".x(async () => {
                message = await channel.GetMessageAsync(queueDefinition.Name, Acknowledgements.Auto);
            });
            "Then the message was retrieved".x(() => {
                message.Should().NotBeNull();
                UTF8.GetString(message!.Value.Body.Span).Should().Be(payload);
            });
            "And the message is no longer in the queue".x(async () => {
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(0);
                });
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void GetMessageWithManualAck(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, String payload, (DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)? message) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            });
            "And the queue has a message".x(async () => {
                await broker.PublishMessage(
                    exchange  : "amq.default",
                    routingKey: queueDefinition.Name,
                    payload   : payload = Lorem.Sentence()
                );

                // Wait a few moments for the message to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client retrieves the message without auto-ack".x(async () => {
                message = await channel.GetMessageAsync(queueDefinition.Name, Acknowledgements.Manual);
            });
            "Then the message was retrieved".x(() => {
                message.Should().NotBeNull();
                UTF8.GetString(message!.Value.Body.Span).Should().Be(payload);
            });
            "And the message is still in the queue".x(async () => {
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void AcknowledgeMessage(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, String payload, (DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)? message) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            });
            "And the queue has a message".x(async () => {
                await broker.PublishMessage(
                    exchange  : "amq.default",
                    routingKey: queueDefinition.Name,
                    payload   : payload = Lorem.Sentence()
                );

                // Wait a few moments for the message to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "And the client has retrieved the message without auto-ack".x(async () => {
                message = await channel.GetMessageAsync(queueDefinition.Name, Acknowledgements.Manual);
            });
            "When the message is acknowledged".x(async () => {
                await channel.AcknowledgeAsync(message!.Value.Delivery.DeliveryTag);
            });
            "Then the message is no longer in the queue".x(async () => {
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(0);
                });
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void RejectMessageWithRequeue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, String payload, (DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)? message) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            });
            "And the queue has a message".x(async () => {
                await broker.PublishMessage(
                    exchange  : "amq.default",
                    routingKey: queueDefinition.Name,
                    payload   : payload = Lorem.Sentence()
                );

                // Wait a few moments for the message to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "And the client has retrieved the message without auto-ack".x(async () => {
                message = await channel.GetMessageAsync(queueDefinition.Name, Acknowledgements.Manual);
            });
            "When the message is rejected with requeue".x(async () => {
                await channel.RejectAsync(message!.Value.Delivery.DeliveryTag, requeue: true);
            });
            "Then the message is requeued".x(async () => {
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
        }

        [Scenario]
        [Example("3.9")]
        [Example("3.8")]
        [Example("3.7")]
        public void RejectMessageWithoutRequeue(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, QueueDefinition queueDefinition, String payload, (DeliveryInfo Delivery, MessageProperties Properties, ReadOnlyMemory<Byte> Body)? message) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a queue declared".x(async () => {
                await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            });
            "And the queue has a message".x(async () => {
                await broker.PublishMessage(
                    exchange  : "amq.default",
                    routingKey: queueDefinition.Name,
                    payload   : payload = Lorem.Sentence()
                );

                // Wait a few moments for the message to show up in the queue...
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(1);
                });
            });
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "And the client has retrieved the message without auto-ack".x(async () => {
                message = await channel.GetMessageAsync(queueDefinition.Name, Acknowledgements.Manual);
            });
            "When the message is rejected with requeue".x(async () => {
                await channel.RejectAsync(message!.Value.Delivery.DeliveryTag, requeue: false);
            });
            "Then the message is not requeued".x(async () => {
                await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                    var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                    messagesCount.Should().Be(0);
                });
            });
        }
    }
}
