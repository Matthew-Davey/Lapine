namespace Lapine.Workbench {
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    class Program {
        static async Task Main() {
            Lapine.Log.LoggerFactory = LoggerFactory.Create(config => {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Debug);
            });

            var resetEvent = new ManualResetEventSlim();

            Console.CancelKeyPress += (_, args) => {
                args.Cancel = true;
                resetEvent.Set();
            };

            var connectionConfiguration = new ConnectionConfiguration(
                endpoints:                 new [] { new IPEndPoint(IPAddress.Loopback, 5672) },
                endpointSelectionStrategy: new RandomEndpointSelectionStrategy(),
                authenticationStrategy:    new PlainAuthenticationStrategy(username: "guest", password: "guest"),
                peerProperties: PeerProperties.Default
                    .WithProduct("Lapine.Workbench")
                    .WithClientProvidedName("Lapine.Workbench")
            );

            var amqpClient = new AmqpClient(connectionConfiguration);

            await amqpClient.ConnectAsync();

            resetEvent.Wait();

            amqpClient.Dispose();
        }
    }
}
