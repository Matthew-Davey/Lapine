namespace Lapine.Workbench;

using Lapine.Client;

using static System.Text.Encoding;

class Program {
    static async Task Main() {
        var connectionConfiguration = ConnectionConfiguration.Default with {
            ConnectionIntegrityStrategy = ConnectionIntegrityStrategy.None,
            PeerProperties = PeerProperties.Default with {
                Product            = "Lapine.Workbench",
                ClientProvidedName = "Lapine.Workbench"
            }
        };

        var amqpClient = new AmqpClient(connectionConfiguration);

        await amqpClient.ConnectAsync();
        var channel = await amqpClient.OpenChannelAsync();
        await channel.DeclareExchangeAsync(ExchangeDefinition.Direct("test.exchange"));
        await channel.DeclareQueueAsync(QueueDefinition.Create("test.queue"));
        await channel.BindQueueAsync(Binding.Create("test.exchange", "test.queue"));

        await channel.PublishAsync(
            exchange    : "test.exchange",
            routingKey  : "#",
            message     : (MessageProperties.Empty, "Test Message 1"u8.ToArray()),
            routingFlags: RoutingFlags.None
        );

        await channel.EnablePublisherConfirms();

        await channel.PublishAsync(
            exchange    : "test.exchange",
            routingKey  : "#",
            message     : (MessageProperties.Empty, "Test Message 2"u8.ToArray()),
            routingFlags: RoutingFlags.None
        );

        await channel.PublishAsync(
            exchange    : "test.exchange",
            routingKey  : "#",
            message     : (MessageProperties.Empty, "Test Message 3"u8.ToArray()),
            routingFlags: RoutingFlags.None
        );

        await Task.Delay(TimeSpan.FromMilliseconds(-1));
        await channel.CloseAsync();
        await amqpClient.DisposeAsync();
    }
}
