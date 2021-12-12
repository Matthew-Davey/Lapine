namespace Lapine.Client;

using static System.Text.Encoding;

public class PublishTests : Faker {
    [Scenario]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void PublishMessage(String brokerVersion, BrokerProxy broker, ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, AmqpClient subject, Channel channel, String payload) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has an exchange bound to a queue".x(async () => {
            await broker.ExchangeDeclareAsync(exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word()));
            await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            await broker.QueueBindAsync(exchangeDefinition.Name, queueDefinition.Name, "#");
        });
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client publishes a message to the exchange".x(async () => {
            await channel.PublishAsync(
                exchange  : exchangeDefinition.Name,
                routingKey: "#",
                message   : (
                    Properties: MessageProperties.Empty,
                    Payload   : UTF8.GetBytes(payload = Lorem.Sentence())
                )
            );
        });
        "Then the message should be routed to the queue".x(async () => {
            await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                var messageCount = await broker.GetMessageCount(queueDefinition.Name);
                messageCount.Should().Be(1);
            });
        });
    }

    [Scenario]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void PublishMessageWithConfirms(String brokerVersion, BrokerProxy broker, ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, AmqpClient subject, Channel channel, String payload) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has an exchange bound to a queue".x(async () => {
            await broker.ExchangeDeclareAsync(exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word()));
            await broker.QueueDeclareAsync(queueDefinition = QueueDefinition.Create(Lorem.Word()));
            await broker.QueueBindAsync(exchangeDefinition.Name, queueDefinition.Name, "#");
        });
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "And publisher confirms are enabled".x(async () => {
            await channel.EnablePublisherConfirms();
        });
        "When the client publishes a message to the exchange".x(async () => {
            await channel.PublishAsync(
                exchange  : exchangeDefinition.Name,
                routingKey: "#",
                message   : (
                    Properties: MessageProperties.Empty,
                    Payload   : UTF8.GetBytes(payload = Lorem.Sentence())
                )
            );
        });
        "Then the message should be routed to the queue".x(async () => {
            await BrokerProxy.ManagementRetryPolicy.ExecuteAsync(async () => {
                var messageCount = await broker.GetMessageCount(queueDefinition.Name);
                messageCount.Should().Be(1);
            });
        });
    }
}
