namespace Lapine.Workbench {
    using System;
    using System.Threading.Tasks;
    using Lapine.Client;
    using Lapine.Protocol;
    using Microsoft.Extensions.Logging;

    using static System.Text.Encoding;

    class Program {
        static async Task Main() {
            Lapine.Log.LoggerFactory = LoggerFactory.Create(config => {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Debug);
            });

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

            await channel.DeclareExchangeAsync(ExchangeDefinition.Create("test.exchange") with {
                Type       = "topic",
                Durability = Durability.Ephemeral,
                AutoDelete = true
            });
            await channel.DeclareQueueAsync(QueueDefinition.Create("test.queue") with {
                Durability = Durability.Ephemeral,
                AutoDelete = true
            });
            //await channel.BindQueueAsync("test.exchange", "test.queue");

            var body = UTF8.GetBytes("test message").AsMemory();
            var properties = BasicProperties.Empty with {
                ContentType     = "text/plain",
                ContentEncoding = "UTF-8",
                Timestamp       = (UInt64)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            await channel.PublishAsync("test.exchange", "#", (properties, body), RoutingFlags.Mandatory);

            await Task.Delay(10000);

            await channel.UnbindQueueAsync("test.exchange", "test.queue");

            Environment.ExitCode = await completion.Task;

            await channel.Close();

            await amqpClient.DisposeAsync();
        }
    }
}
