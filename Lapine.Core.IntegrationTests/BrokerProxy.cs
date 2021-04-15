namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using CliWrap;
    using CliWrap.Buffered;
    using Newtonsoft.Json.Linq;

    public class BrokerProxy : IAsyncDisposable {
        public enum ConnectionState {
            Starting,
            Tuning,
            Opening,
            Running,
            Flow,
            Blocking,
            Blocked,
            Closing,
            Closed
        }

        public record Connection(String AuthMechanism, String User, ConnectionState State, PeerProperties PeerProperties);

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
        public async Task<ConnectionConfiguration> GetConnectionConfigurationAsync() =>
            ConnectionConfiguration.Default with {
                Endpoints = new [] { new IPEndPoint(await GetIPAddressAsync(), 5672) }
            };

        public async IAsyncEnumerable<Connection> GetConnections() {
            var process = await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add("list_connections auth_mechanism user state client_properties", escape: false)
                    .Add("--formatter json", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            var connections = JArray.Parse(process.StandardOutput);

            foreach (var connection in connections) {
                var authMechanism = (String)connection.SelectToken("$.auth_mechanism");
                var user          = (String)connection.SelectToken("$.user");
                var state         = Enum.Parse<ConnectionState>((String)connection.SelectToken("$.state"), ignoreCase: true);

                yield return new Connection(authMechanism, user, state, PeerProperties.Empty with {
                    Product            = (String)connection.SelectToken("$.client_properties[0][2]"),
                    Version            = (String)connection.SelectToken("$.client_properties[1][2]"),
                    Platform           = (String)connection.SelectToken("$.client_properties[2][2]"),
                    Copyright          = (String)connection.SelectToken("$.client_properties[3][2]"),
                    Information        = (String)connection.SelectToken("$.client_properties[4][2]"),
                    ClientProvidedName = (String)connection.SelectToken("$.client_properties[5][2]")
                });
            }
        }

        public async ValueTask AddUser(String username, String password) =>
            await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"add_user {username} {password}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();

        public async ValueTask AddVirtualHost(String virtualHost) =>
            await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"add_vhost {virtualHost}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();

        public async ValueTask SetPermissions(String virtualHost, String user, String configure = ".*", String write = ".*", String read = ".*") =>
            await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"set_permissions -p {virtualHost} {user} {configure} {write} {read}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();

        public async ValueTask DisposeAsync() =>
            await Cli.Wrap("docker")
                .WithArguments($"stop {_container}")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();
    }
}
