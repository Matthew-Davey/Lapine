namespace Lapine;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
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
    public record Channel(String Name, Int32 Number, String User, String Vhost, Boolean Confirm);

    static readonly Command Docker = new Command("docker")
        .WithValidation(CommandResultValidation.ZeroExitCode);

    readonly String _container;

    BrokerProxy(String containerId) =>
        _container = containerId;

    static public async ValueTask<BrokerProxy> StartAsync(String brokerVersion, Boolean enableManagement = false) {
        if (enableManagement)
            brokerVersion = $"{brokerVersion}-management";

        // Start a RabbitMQ container...
        var process = await Docker
            .WithArguments($"run --detach --rm rabbitmq:{brokerVersion}-alpine")
            .ExecuteBufferedAsync();

        var container = process.StandardOutput[..12];

        await Task.Delay(1000);
        await Handle<CommandExecutionException>()
            .WaitAndRetryAsync(10, _ => TimeSpan.FromMilliseconds(500))
            .ExecuteAsync(async () => {
                await Docker
                    .WithArguments($"exec {container} rabbitmq-diagnostics check_running")
                    .ExecuteAsync();
            });

        return new BrokerProxy(container);
    }

    /// <summary>
    /// Gets the IP address of the running broker.
    /// </summary>
    public async ValueTask<IPAddress> GetIPAddressAsync() {
        var process = await Docker
            .WithArguments($"inspect {_container} " + "--format=\"{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}\"")
            .ExecuteBufferedAsync();

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
        var process = await Docker
            .WithArguments($"exec {_container} rabbitmqctl list_connections auth_mechanism user state client_properties --formatter json")
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
                ClientProvidedName = (String)connection.SelectToken("$.client_properties[5][2]"),
                Capabilities       = ParseCapabilities((JArray)connection.SelectToken("$.client_properties[6][2]"))
            });
        }

        static ClientCapabilities ParseCapabilities(JArray capabilitiesJson) {
            var capabilities = ClientCapabilities.None;

            foreach (var capability in capabilitiesJson) {
                switch ((String)capability[0]) {
                    case "basic_nack": {
                        capabilities = capabilities with { BasicNack = (Boolean)capability[2] };
                        break;
                    }
                    case "publisher_confirms": {
                        capabilities = capabilities with { PublisherConfirms = (Boolean)capability[2] };
                        break;
                    }
                }
            }

            return capabilities;
        }
    }

    public async IAsyncEnumerable<Channel> GetChannelsAsync() {
        var process = await Docker
            .WithArguments($"exec {_container} rabbitmqctl list_channels name number user vhost confirm --formatter json")
            .ExecuteBufferedAsync();

        var channels = JArray.Parse(process.StandardOutput);

        foreach (var channel in channels) {
            var name    = (String)channel.SelectToken("$.name");
            var number  = (Int32)channel.SelectToken("$.number");
            var user    = (String)channel.SelectToken("$.user");
            var vhost   = (String)channel.SelectToken("$.vhost");
            var confirm = (Boolean)channel.SelectToken("$.confirm");

            yield return new Channel(name, number, user, vhost, confirm);
        }
    }

    public async IAsyncEnumerable<ExchangeDefinition> GetExchangesAsync() {
        var process = await Docker
            .WithArguments($"exec {_container} rabbitmqctl list_exchanges name type durable auto_delete arguments --formatter json")
            .ExecuteBufferedAsync();

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
        var process = await Docker
            .WithArguments($"exec {_container} rabbitmqctl list_queues name auto_delete exclusive durable arguments --formatter json")
            .ExecuteBufferedAsync();

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
        await Docker
            .WithArguments($"exec {_container} rabbitmqctl add_user {username} {password}")
            .ExecuteAsync();

    public async ValueTask AddVirtualHostAsync(String virtualHost) =>
        await Docker
            .WithArguments($"exec {_container} rabbitmqctl add_vhost {virtualHost}")
            .ExecuteAsync();

    public async ValueTask SetPermissionsAsync(String virtualHost, String user, String configure = ".*", String write = ".*", String read = ".*") =>
        await Docker
            .WithArguments($"exec {_container} rabbitmqctl set_permissions -p {virtualHost} {user} {configure} {write} {read}")
            .ExecuteAsync();

    public async ValueTask ExchangeDeclareAsync(ExchangeDefinition definition) {
        var durable = definition.Durability switch {
            Durability.Durable   => "true",
            Durability.Transient => "false",
            _                    => "false"
        };

        await Docker
            .WithArguments($"exec {_container} rabbitmqadmin declare exchange name={definition.Name} type={definition.Type} durable={durable}")
            .ExecuteBufferedAsync();
    }

    public async ValueTask QueueDeclareAsync(QueueDefinition definition) {
        var durable = definition.Durability switch {
            Durability.Durable   => "true",
            Durability.Transient => "false",
            _                    => "false"
        };

        var autoDelete = definition.AutoDelete switch {
            true  => "true",
            false => "false"
        };

        await Docker
            .WithArguments($"exec {_container} rabbitmqadmin declare queue name={definition.Name} durable={durable} auto_delete={autoDelete}")
            .ExecuteBufferedAsync();
    }

    public async ValueTask QueueBindAsync(String exchange, String queue, String routingKey) =>
        await Docker
            .WithArguments($"exec {_container} rabbitmqadmin declare binding source={exchange} destination={queue} routing_key={routingKey}")
            .ExecuteAsync();

    public async ValueTask PublishMessage(String exchange, String routingKey, String payload) =>
        await Docker
            .WithArguments($"exec {_container} rabbitmqadmin publish exchange={exchange} routing_key={routingKey} payload=\"{payload}\"")
            .ExecuteAsync();

    public async ValueTask<Int32> GetMessageCount(String queue) {
        var process = await Docker
            .WithArguments($"exec {_container} rabbitmqctl list_queues --formatter json")
            .ExecuteBufferedAsync();

        var array = JArray.Parse(process.StandardOutput);
        return array
            .Where(x => (String)((JObject)x).SelectToken("$.name") == queue)
            .Select(x => (Int32)((JObject)x).SelectToken("$.messages"))
            .SingleOrDefault();
    }

    public async ValueTask DisposeAsync() {
        await Docker
            .WithArguments($"stop {_container}")
            .ExecuteAsync();

        GC.SuppressFinalize(this);
    }
}
