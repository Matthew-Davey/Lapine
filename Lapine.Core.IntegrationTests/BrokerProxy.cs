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
        public record Channel(String Name, Int32 Number, String User, String Vhost);

        static readonly IAsyncPolicy CommandRetryPolicy =
            Handle<CommandExecutionException>()
                .WaitAndRetryAsync(5, _ => TimeSpan.FromMilliseconds(100));

        readonly String _container;

        private BrokerProxy(String containerId) =>
            _container = containerId;

        static public async ValueTask<BrokerProxy> StartAsync(String brokerVersion, Boolean enableManagement = false) {
            if (enableManagement)
                brokerVersion = $"{brokerVersion}-management";

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

            var broker = new BrokerProxy(container);

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
                AuthenticationStrategy      = new PlainAuthenticationStrategy("guest", "guest"),
                Endpoints                   = new [] { new IPEndPoint(await GetIPAddressAsync(), 5672) },
                ConnectionIntegrityStrategy = Debugger.IsAttached switch {
                    // Disable connection integrity checks when debugging, we don't want the connection to be terminated
                    // while we're stepping through the code...
                    true  => ConnectionIntegrityStrategy.None,
                    false => ConnectionIntegrityStrategy.Default
                }
            };

        public async IAsyncEnumerable<Connection> GetConnectionsAsync() {
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

        public async IAsyncEnumerable<Channel> GetChannelsAsync() {
            var process = await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add("list_channels name number user vhost", escape: false)
                    .Add("--formatter json", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

            var channels = JArray.Parse(process.StandardOutput);

            foreach (var channel in channels) {
                var name   = (String)channel.SelectToken("$.name");
                var number = (Int32)channel.SelectToken("$.number");
                var user   = (String)channel.SelectToken("$.user");
                var vhost  = (String)channel.SelectToken("$.vhost");

                yield return new Channel(name, number, user, vhost);
            }
        }

        public async IAsyncEnumerable<ExchangeDefinition> GetExchangesAsync() {
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
                        JObject obj         => obj.ToObject<ImmutableDictionary<String, Object>>(),
                        _                   => throw new Exception("Unexpected rabbitmqctl output")
                    },
                };
            }
        }

        public async IAsyncEnumerable<QueueDefinition> GetQueuesAsync() {
            var process = await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add("list_queues name auto_delete exclusive durable arguments", escape: false)
                    .Add("--formatter json", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

            var queues = JArray.Parse(process.StandardOutput);

            foreach (var queue in queues) {
                var name       = (String)queue.SelectToken("$.name");
                var autoDelete = (Boolean)queue.SelectToken("$.auto_delete");
                var exclusive  = (Boolean)queue.SelectToken("$.exclusive");
                var durable    = (Boolean)queue.SelectToken("$.durable");
                var arguments  = queue.SelectToken("$.arguments");

                yield return QueueDefinition.Create(name) with {
                    AutoDelete = autoDelete,
                    Durability = durable switch {
                        true  => Durability.Durable,
                        false => Durability.Transient
                    },
                    Exclusive  = exclusive,
                    Arguments  = arguments switch {
                        JArray { Count: 0 } => ImmutableDictionary<String, Object>.Empty,
                        JObject obj         => obj.ToObject<ImmutableDictionary<String, Object>>(),
                        _                   => throw new Exception("Unexpected rabbitmqctl output")
                    },
                };
            }
        }

        public async ValueTask AddUserAsync(String username, String password) =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"add_user {username} {password}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

        public async ValueTask AddVirtualHostAsync(String virtualHost) =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"add_vhost {virtualHost}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

        public async ValueTask SetPermissionsAsync(String virtualHost, String user, String configure = ".*", String write = ".*", String read = ".*") =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqctl")
                    .Add($"set_permissions -p {virtualHost} {user} {configure} {write} {read}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );

        public async ValueTask ExchangeDeclareAsync(ExchangeDefinition definition) =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqadmin")
                    .Add($"declare exchange name={definition.Name} type={definition.Type} durable={definition.Durability switch { Durability.Durable => "true", _ => "false" } }", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

        public async ValueTask QueueDeclareAsync(QueueDefinition definition) =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments(arguments => arguments
                    .Add("exec")
                    .Add(_container)
                    .Add("rabbitmqadmin")
                    .Add($"declare queue name={definition.Name} durable={definition.Durability switch { Durability.Durable => "true", _ => "false" } } auto_delete={(definition.AutoDelete ? "true" : "false")}", escape: false))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync()
            );

        public async ValueTask DisposeAsync() =>
            await CommandRetryPolicy.ExecuteAsync(async () => await Cli.Wrap("docker")
                .WithArguments($"stop {_container}")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync()
            );
    }
}
