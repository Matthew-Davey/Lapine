namespace Lapine.Workbench {
    using System;
    using System.Threading.Tasks;
    using Lapine.Client;

    class Program {
        static async Task Main() {
            var connectionConfiguration = ConnectionConfiguration.Default with {
                ConnectionIntegrityStrategy = ConnectionIntegrityStrategy.None with {
                    HeartbeatFrequency = TimeSpan.FromSeconds(5),
                    KeepAliveSettings = (
                        ProbeTime: TimeSpan.FromSeconds(5),
                        RetryInterval: TimeSpan.FromSeconds(1),
                        RetryCount: 5
                    )
                },
                ConnectionTimeout = TimeSpan.MaxValue,
                PeerProperties    = PeerProperties.Default with {
                    Product            = "Lapine.Workbench",
                    ClientProvidedName = "Lapine.Workbench"
                }
            };

            var amqpClient = new AmqpClient(connectionConfiguration);

            await amqpClient.ConnectAsync();
            var channel = await amqpClient.OpenChannelAsync();
            await channel.DeclareExchangeAsync(ExchangeDefinition.Direct("test.exchange"));
            await channel.DeclareQueueAsync(QueueDefinition.Create("test.queue"));
            await channel.UnbindQueueAsync(Binding.Create("test.exchange", "test.queue"));
            await Task.Delay(TimeSpan.FromMilliseconds(-1));
            await channel.CloseAsync();
            await amqpClient.DisposeAsync();
        }
    }
}
