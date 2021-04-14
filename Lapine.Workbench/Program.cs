namespace Lapine.Workbench {
    using System;
    using System.Threading.Tasks;
    using Lapine.Client;

    using static System.Text.Encoding;

    class Program {
        static async Task Main() {
            var completion = new TaskCompletionSource<Int32>();

            Console.CancelKeyPress += (_, args) => {
                args.Cancel = true;
                completion.SetResult(0);
            };

            var connectionConfiguration = ConnectionConfiguration.Default with {
                HeartbeatFrequency = TimeSpan.FromSeconds(10),
                ConnectionTimeout = TimeSpan.MaxValue,
                PeerProperties = PeerProperties.Default with {
                    Product            = "Lapine.Workbench",
                    ClientProvidedName = "Lapine.Workbench"
                }
            };

            var amqpClient = new AmqpClient(connectionConfiguration);

            await amqpClient.ConnectAsync();

            var channel = await amqpClient.OpenChannelAsync();
            await channel.SetPrefetchLimit(1, PrefetchLimitScope.Channel);

            await channel.DeclareExchangeAsync(ExchangeDefinition.Create("test.exchange") with {
                Type       = "topic",
                Durability = Durability.Ephemeral,
                AutoDelete = true
            });
            await channel.DeclareQueueAsync(QueueDefinition.Create("test.queue") with {
                Durability = Durability.Ephemeral,
                AutoDelete = true
            });
            await channel.BindQueueAsync("test.exchange", "test.queue");
            await channel.PurgeQueueAsync("test.queue");

            var body = UTF8.GetBytes("test message").AsMemory();
            var properties = MessageProperties.Empty with {
                ContentType     = "text/plain",
                ContentEncoding = UTF8.WebName,
                DeliveryMode    = DeliveryMode.Persistent,
                Timestamp       = DateTimeOffset.UtcNow
            };
            await channel.PublishAsync("test.exchange", "#", (properties, body));

            await Task.Delay(10000);

            var message = await channel.GetMessage("test.queue", false);

            await channel.UnbindQueueAsync("test.exchange", "test.queue");

            Environment.ExitCode = await completion.Task;

            await channel.Close();

            await amqpClient.DisposeAsync();
        }
    }
}
