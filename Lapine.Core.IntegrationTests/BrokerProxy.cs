namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
    using Lapine.Client;
    using CliWrap;
    using CliWrap.Buffered;
    using CliWrap.Exceptions;
    using Newtonsoft.Json.Linq;
    using Polly;

    using static Polly.Policy;

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

        static readonly IAsyncPolicy CommandRetryPolicy =
            Handle<CommandExecutionException>()
                .WaitAndRetryAsync(5, _ => TimeSpan.FromMilliseconds(100));

        readonly String _container;
        readonly String _username;
        readonly String _password;

        private BrokerProxy(String containerId, String username, String password) {
            _container = containerId;
            _username = username;
            _password = password;
        }

        public String Username => _username;

        static public async ValueTask<BrokerProxy> StartAsync(String brokerVersion, String username = "lapine", String password = "lapine") {
            // Start a RabbitMQ container...
            var process = await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("run")
                    .Add("--detach")
                    .Add("--rm")
                    .Add($"rabbitmq:{brokerVersion}"))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

            var container = process.StandardOutput[..12];

            // Wait a few seconds for the container to boot...
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Wait up to 60 seconds for RabbitMQ to start up...
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(container)
                    .Add("rabbitmqctl await_startup --timeout 60", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

            var broker = new BrokerProxy(container, username, password);
            await broker.AddUser(username, password);
            await broker.SetPermissions("/", username);

            return broker;
        }

        /// <summary>
        /// Gets the IP address of the running broker.
        /// </summary>
        public async ValueTask<IPAddress> GetIPAddressAsync() {
            var process = await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("inspect")
                    .Add(_container)
                    .Add("--format=\"{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}\"", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

            return IPAddress.Parse(process.StandardOutput.TrimEnd());
        }

        /// <summary>
        /// Gets a `ConnectionConfiguration` instance which is pre-configured to connect to the broker.
        /// </summary>
        public async Task<ConnectionConfiguration> GetConnectionConfigurationAsync() =>
            ConnectionConfiguration.Default with {
                AuthenticationStrategy = new PlainAuthenticationStrategy(_username, _password),
                Endpoints              = new [] { new IPEndPoint(await GetIPAddressAsync(), 5672) },
                HeartbeatFrequency     = Debugger.IsAttached switch {
                    true  => TimeSpan.Zero, // Zero disables heartbeats - see https://www.rabbitmq.com/heartbeats.html#heartbeats-timeout
                    false => ConnectionConfiguration.DefaultHeartbeatFrequency
                }
            };

        public async IAsyncEnumerable<Connection> GetConnections() {
            var process = await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add("list_connections auth_mechanism user state client_properties", escape: false)
                    .Add("--formatter json", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

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

        public async IAsyncEnumerable<ExchangeDefinition> GetExchanges() {
            var process = await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add("list_exchanges name type durable auto_delete arguments", escape: false)
                    .Add("--formatter json", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

            var exchanges = JArray.Parse(process.StandardOutput);

            foreach (var exchange in exchanges) {
                var name       = (String)exchange.SelectToken("$.name");
                var type       = (String)exchange.SelectToken("$.type");
                var durable    = (Boolean)exchange.SelectToken("$.durable");
                var autoDelete = (Boolean)exchange.SelectToken("$.auto_delete");
                var arguments  = exchange.SelectToken("$.arguments");

                yield return ExchangeDefinition.Create(name, type) with {
                    AutoDelete = autoDelete,
                    Durability = durable switch {
                        true  => Durability.Durable,
                        false => Durability.Transient
                    },
                    Arguments  = arguments switch {
                        JArray { Count: 0 } => ImmutableDictionary<String, Object>.Empty,
                        JObject obj         => obj.ToObject<Dictionary<String, Object>>(),
                        _                   => throw new Exception("Unexpected rabbitmqctl output")
                    },
                };
            }
        }

        public async ValueTask AddUser(String username, String password) =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"add_user {username} {password}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

        public async ValueTask AddVirtualHost(String virtualHost) =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"add_vhost {virtualHost}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

        public async ValueTask SetPermissions(String virtualHost, String user, String configure = ".*", String write = ".*", String read = ".*") =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"set_permissions -p {virtualHost} {user} {configure} {write} {read}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

        public async ValueTask DisposeAsync() =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments($"stop {_container}")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );
    }
}
