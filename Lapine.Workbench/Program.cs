namespace Lapine.Workbench {
    using System;
    using System.Net;
    using System.Threading;
    using Lapine.Agents;
    using Lapine.Agents.Middleware;
    using Microsoft.Extensions.Logging;
    using Proto;

    using static Lapine.Agents.Messages;

    class Program {
        static void Main() {
            Lapine.Log.LoggerFactory = LoggerFactory.Create(config => {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Debug);
            });

            var resetEvent = new ManualResetEventSlim();

            Console.CancelKeyPress += (_, args) => {
                args.Cancel = true;
                resetEvent.Set();
            };

            var context = new RootContext();
            var connectionConfiguration = new ConnectionConfiguration(
                endpoints:                 new [] { new IPEndPoint(IPAddress.Loopback, 5672) },
                endpointSelectionStrategy: new InOrderEndpointSelectionStrategy(),
                peerProperties: PeerProperties.Default
                    .WithProduct("Lapine.Workbench")
                    .WithClientProvidedName("Lapine.Workbench")
            );

            var client = context.SpawnNamed(
                name: "amqp-client",
                props: Props.FromProducer(() => new AmqpClientAgent(connectionConfiguration))
                    .WithChildSupervisorStrategy(new AllForOneStrategy((pid, reason) => SupervisorDirective.Stop, 1, TimeSpan.FromSeconds(1)))
                    .WithContextDecorator(LoggingContextDecorator.Create)
            );

            context.Send(client, (Connect));

            resetEvent.Wait();
        }
    }
}
