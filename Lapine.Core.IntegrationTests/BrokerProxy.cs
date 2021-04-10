namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using CliWrap;
    using CliWrap.Buffered;
    using Newtonsoft.Json.Linq;

    public class BrokerProxy : IAsyncDisposable {
        public record Connection(String User, String State, String? Product, String? Version, String? Name);

        readonly String _container;

        private BrokerProxy(String containerId) =>
            _container = containerId;

        static public async ValueTask<BrokerProxy> StartAsync(String brokerVersion) {
            // Start a RabbitMQ container...
            var process = await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("run")
                    .Add("--detach")
                    .Add("--rm")
                    .Add($"rabbitmq:{brokerVersion}"))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            var container = process.StandardOutput[..12];

            // Wait a few seconds for the container to boot...
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Wait up to 60 seconds for RabbitMQ to start up...
            await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(container)
                    .Add("rabbitmqctl await_startup --timeout 60", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();

            return new BrokerProxy(container);
        }

        /// <summary>
        /// Gets the IP address of the running broker.
        /// </summary>
        public async ValueTask<IPAddress> GetIPAddressAsync() {
            var process = await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("inspect")
                    .Add(_container)
                    .Add("--format=\"{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}\"", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            return IPAddress.Parse(process.StandardOutput.TrimEnd());
        }

        /// <summary>
        /// Gets a `ConnectionConfiguration` instance which is pre-configured to connect to the broker.
        /// </summary>
        public async Task<ConnectionConfiguration> GetConnectionConfigurationAsync() {
            var ip = await GetIPAddressAsync();

            return ConnectionConfiguration.Default with {
                Endpoints = new [] { new IPEndPoint(ip, 5672) }
            };
        }

        public async IAsyncEnumerable<Connection> GetConnectionsAsync() {
            var process = await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add("list_connections user state client_properties", escape: false)
                    .Add("--formatter json", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            var connections = JArray.Parse(process.StandardOutput);

            foreach (var connection in connections) {
                var user    = (String)connection.SelectToken("$.user");
                var state   = (String)connection.SelectToken("$.state");
                var product = (String)connection.SelectToken("$.client_properties[0][2]");
                var version = (String)connection.SelectToken("$.client_properties[1][2]");
                var name    = (String)connection.SelectToken("$.client_properties[5][2]");

                yield return new Connection(user, state, product, version, name);
            }
        }

        public async ValueTask DisposeAsync() {
            await Cli.Wrap("docker")
                .WithArguments($"stop {_container}")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();
        }
    }
}
