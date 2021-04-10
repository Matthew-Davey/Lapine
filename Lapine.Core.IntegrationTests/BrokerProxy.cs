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
            var result = await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("inspect")
                    .Add("--format=\"{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}\"", escape: false)
                    .Add(_container!))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            return IPAddress.Parse(result.StandardOutput.TrimEnd());
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

        public async ValueTask<IList<Connection>> GetConnectionsAsync() {
            var result = await Cli.Wrap("docker")
                .WithArguments($"exec {_container} rabbitmqctl list_connections user state client_properties --formatter json")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            var json = JArray.Parse(result.StandardOutput);
            var results = new List<Connection>();
            foreach (var token in json) {
                var user = token.SelectToken("$.user").Value<String>();
                var state = token.SelectToken("$.state").Value<String>();
                var product = token.SelectToken("$.client_properties[0][2]").Value<String>();
                var version = token.SelectToken("$.client_properties[1][2]").Value<String>();
                var name = token.SelectToken("$.client_properties[5][2]").Value<String>();

                results.Add(new Connection(user, state, product, version, name));
            }

            return results;
        }

        public async ValueTask DisposeAsync() {
            await Cli.Wrap("docker")
                .WithArguments($"stop {_container}")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();
        }
    }
}
