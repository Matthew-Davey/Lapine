namespace Lapine.Client;

public class QueueTests : Faker {
    [Scenario]
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
}
