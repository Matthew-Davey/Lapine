namespace Lapine.Workbench {
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

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

            var connectionConfiguration = ConnectionConfiguration.Default
                .WithPeerProperties(PeerProperties.Default
                    .WithProduct("Lapine.Workbench")
                    .WithClientProvidedName("Lapine.Workbench")
                );

            var amqpClient = new AmqpClient(connectionConfiguration);

            await amqpClient.ConnectAsync();

            Environment.ExitCode = await completion.Task;

            await amqpClient.DisposeAsync();
        }
    }
}
