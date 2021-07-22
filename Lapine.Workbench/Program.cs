namespace Lapine.Workbench {
    using System;
    using System.Threading.Tasks;
    using Lapine.Client;

    using static System.Text.Encoding;

    class Program {
        static async Task Main() {
            var connectionConfiguration = ConnectionConfiguration.Default with {
                HeartbeatFrequency = TimeSpan.FromSeconds(60),
                ConnectionTimeout  = TimeSpan.MaxValue,
                PeerProperties     = PeerProperties.Default with {
                    Product            = "Lapine.Workbench",
                    ClientProvidedName = "Lapine.Workbench"
                }
            };

            var amqpClient = new AmqpClient(connectionConfiguration);

            await amqpClient.ConnectAsync();

            var channel = await amqpClient.OpenChannelAsync();

            await channel.DeclareExchangeAsync(ExchangeDefinition.Direct("test.exchange") with {
                Durability = Durability.Transient,
                AutoDelete = true
            });
            await channel.DeclareQueueAsync(QueueDefinition.Create("test.queue") with {
                Durability = Durability.Durable,
                AutoDelete = false
            });
            await channel.BindQueueAsync("test.exchange", "test.queue");
            await channel.SetPrefetchLimitAsync(100, PrefetchLimitScope.Consumer);
            // await channel.PurgeQueueAsync("test.queue");

            // var body = UTF8.GetBytes("test message").AsMemory();
            // var properties = MessageProperties.Empty with {
            //     ContentType     = "text/plain",
            //     ContentEncoding = UTF8.WebName,
            //     DeliveryMode    = DeliveryMode.Persistent,
            //     Timestamp       = DateTimeOffset.UtcNow
            // };
            // await channel.PublishAsync("test.exchange", "#", (properties, body));

            for (var i = 0; i < 100_000; i++) {
                await channel.PublishAsync("test.exchange", "#", (MessageProperties.Empty, UTF8.GetBytes($"TEST MESSAGE {i}")));
            }

            string consumerTag = await channel.ConsumeAsync("test.queue", ConsumerConfiguration.Create(
                (DeliveryInfo deliveryInfo, MessageProperties properties, ReadOnlyMemory<Byte> body) => {
                    Console.WriteLine(UTF8.GetString(body.Span));
                    return Task.CompletedTask;
                }) with {
                    MaxDegreeOfParallelism = 16
                }
            );

            await Task.Delay(TimeSpan.FromMilliseconds(-1));

            // await Task.Delay(10000);

            // var message = await channel.GetMessage("test.queue", false);

            // await channel.UnbindQueueAsync("test.exchange", "test.queue");

            await channel.CloseAsync();

            await amqpClient.DisposeAsync();
        }
    }
}
