module BrokerContainer

open System
open System.Diagnostics
open System.Net
open System.Text.Json.Nodes
open System.Threading
open Amqp
open AmqpTypes
open Testcontainers.RabbitMq

type ConnectionState =
    | Starting = 0
    | Tuning = 1
    | Opening = 2
    | Running = 3
    | Flow = 4
    | Blocking = 5
    | Blocked = 6
    | Closing = 7
    | Closed = 8

type Connection =
    { AuthMechanism: string
      Username: string
      State: ConnectionState }

type Channel =
    { Name: string
      Number: int
      User: string
      VirtualHost: string
      PublisherConfirms: bool }

let start version =
    let broker =
        RabbitMqBuilder()
            .WithImage($"rabbitmq:%s{version}-alpine")
            .WithAutoRemove(true)
            .WithUsername("guest")
            .WithPassword("guest")
            .Build()

    task {
        do! broker.StartAsync()
        return broker
    }

let getConnectionConfiguration (container: RabbitMqContainer) =
    { ConnectionConfiguration.default' with
        AuthenticationStrategy = AuthenticationStrategy.PlainText("guest", "guest")
        EndPoints = [ IPEndPoint(IPAddress.Parse(container.IpAddress), ConnectionConfiguration.DefaultPort) ]
        ConnectionIntegrityStrategy =
            // Disable connection integrity checks when debugging, we don't want the connection to be terminated
            // while we're stepping through the code...
            if Debugger.IsAttached then
                ConnectionIntegrityStrategy.None
            else
                ConnectionConfiguration.DefaultConnectionIntegrityStrategy }

let addUser username password (container: RabbitMqContainer) =
    task {
        let! _ = container.ExecAsync([| "rabbitmqctl"; "add_user"; username; password |], CancellationToken.None)

        return ()
    }

let setPermissions virtualHost username configure write read (container: RabbitMqContainer) =
    task {
        let! _ =
            container.ExecAsync(
                [| "rabbitmqctl"
                   "set_permissions"
                   "-p"
                   virtualHost
                   username
                   configure
                   write
                   read |],
                CancellationToken.None
            )

        return ()
    }

let getConnections (container: RabbitMqContainer) =
    task {
        let! result =
            container.ExecAsync(
                [| "rabbitmqctl"
                   "list_connections"
                   "auth_mechanism"
                   "user"
                   "state"
                   "client_properties"
                   "--formatter"
                   "json" |],
                CancellationToken.None
            )

        let connections = JsonNode.Parse(result.Stdout).AsArray()

        return
            seq {
                for connection in connections do
                    let authMechanism = connection["auth_mechanism"].GetValue<string>()
                    let user = connection["user"].GetValue<string>()

                    let state =
                        connection["state"].GetValue<string>()
                        |> (fun value -> Enum.Parse<ConnectionState>(value, ignoreCase = true))

                    yield
                        { AuthMechanism = authMechanism
                          Username = user
                          State = state }
            }
    }

let addVirtualHost name (container: RabbitMqContainer) =
    task {
        let! _ = container.ExecAsync([| "rabbitmqctl"; "add_vhost"; name |])
        return ()
    }

let getChannels (container: RabbitMqContainer) =
    task {
        let! result =
            container.ExecAsync
                [| "rabbitmqctl"
                   "list_channels"
                   "name"
                   "number"
                   "user"
                   "vhost"
                   "confirm"
                   "--formatter"
                   "json" |]

        let channels = JsonNode.Parse(result.Stdout).AsArray()

        return
            seq {
                for channel in channels do
                    yield
                        { Name = channel["name"].GetValue<string>()
                          Number = channel["number"].GetValue<int32>()
                          User = channel["user"].GetValue<string>()
                          VirtualHost = channel["vhost"].GetValue<string>()
                          PublisherConfirms = channel["confirm"].GetValue<bool>() }
            }
    }
