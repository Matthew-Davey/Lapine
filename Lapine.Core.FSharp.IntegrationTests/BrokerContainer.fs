[<RequireQualifiedAccess>]
module BrokerContainer

open System.Diagnostics
open System.Net
open System.Threading
open Amqp
open AmqpTypes
open Testcontainers.RabbitMq
    
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
        AuthenticationStrategy = AuthenticationStrategy.PlainText ("guest", "guest")
        EndPoints = [ IPEndPoint(IPAddress.Parse(container.IpAddress), ConnectionConfiguration.DefaultPort) ]
        ConnectionIntegrityStrategy =
             // Disable connection integrity checks when debugging, we don't want the connection to be terminated
             // while we're stepping through the code...
             if Debugger.IsAttached then ConnectionIntegrityStrategy.None else ConnectionConfiguration.DefaultConnectionIntegrityStrategy
    }
    
let addUser username password (container: RabbitMqContainer) = task {
    let! _ = container.ExecAsync([|"rabbitmqctl"; "add_user"; username; password|], CancellationToken.None)
    
    return ()
}

let setPermissions virtualHost username configure write read (container: RabbitMqContainer) = task {
    let! _ = container.ExecAsync([|"rabbitmqctl"; "set_permissions"; "-p"; virtualHost; username; configure; write; read|], CancellationToken.None)
    
    return ()
}