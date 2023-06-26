namespace Lapine.Client;

using static System.Text.Encoding;

public class ConsumeTests : Faker {
    [Scenario]
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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
    [Example("3.11")]
    [Example("3.10")]
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

    [Scenario]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void PurgeQueue(String brokerVersion, BrokerProxy broker, QueueDefinition queueDefinition, AmqpClient subject, Channel channel) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has a queue declared".x(async () => {
            await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
        });
        "And the queue has 10 messages".x(async () => {
            await Task.WhenAll(
                Enumerable.Range(0, 10)
                    .Select(_ =>
                        broker.PublishMessage(
                            exchange  : "amq.default",
                            routingKey: queueDefinition.Name,
                            payload   : Lorem.Sentence()
                        ).AsTask()
                    )
            );

            // Wait a few moments for the message to show up in the queue...
            await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                messagesCount.Should().Be(10);
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
        "Then the queue is empty".x(async () => {
            await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                var messagesCount = await broker.GetMessageCount(queueDefinition.Name);
                messagesCount.Should().Be(0);
            });
        });
    }
}
