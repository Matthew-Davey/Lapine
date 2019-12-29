namespace Lapine.Workbench {
    using System;
    using System.Net;
    using System.Threading;
    using Lapine.Agents;
    using Lapine.Agents.Commands;
    using Lapine.Agents.Middleware;
    using Microsoft.Extensions.Logging;
    using Proto;

    class Program {
        static void Main() {
            Lapine.Log.LoggerFactory = LoggerFactory.Create(config => {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Debug);
            });
            Proto.Log.SetLoggerFactory(Lapine.Log.LoggerFactory);

            var resetEvent = new ManualResetEventSlim();

            Console.CancelKeyPress += (_, args) => {
                args.Cancel = true;
                resetEvent.Set();
            };

            var context = new RootContext();
            var socketAgent = context.SpawnNamed(
                Props.FromProducer(() => new SocketAgent())
                    .WithContextDecorator(context => new LoggingContextDecorator(context)),
                "socket"
            );

            context.Send(socketAgent, new SocketConnect(IPAddress.Loopback, 5672));

            resetEvent.Wait();
        }
    }
}
