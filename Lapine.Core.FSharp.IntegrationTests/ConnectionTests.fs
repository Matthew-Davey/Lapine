module ``Connection Tests``

open System
open System.Net
open AgentMessages
open AmqpTypes
open Amqp
open FsUnit
open FsUnit.CustomMatchers
open Xunit
open AmqpClient
open BrokerContainer

let allSupportedVersions () =
    seq {
        [| "4.1" |]
        [| "4.0" |]
        [| "3.13" |]
        [| "3.12" |]
        [| "3.11" |]
        [| "3.10" |]
        [| "3.9" |]
        [| "3.8" |]
        [| "3.7" |]
    }

[<Theory>]
[<MemberData(nameof allSupportedVersions)>]
let ``Connect as guest`` brokerVersion =
    task {
        use! broker = BrokerContainer.start brokerVersion

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ] }

        let client = AmqpClient(connectionConfiguration)

        let! connectResult = client.Connect()

        connectResult |> should be (ofCase <@ Connected @>)

        let! serverConnections = BrokerContainer.getConnections broker

        serverConnections
        |> should
            contain
            { AuthMechanism = "PLAIN"
              Username = "guest"
              State = ConnectionState.Running }
    }

[<Theory>]
[<MemberData(nameof allSupportedVersions)>]
let ``Connect as user`` brokerVersion =
    task {
        // TODO: Use randomized inputs...
        let username = "foo"
        let password = "bar"

        use! broker = BrokerContainer.start brokerVersion
        do! BrokerContainer.addUser username password broker
        do! BrokerContainer.setPermissions "/" username ".*" ".*" ".*" broker

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ]
                AuthenticationStrategy = PlainText(username, password) }

        let client = AmqpClient(connectionConfiguration)

        let! connectResult = client.Connect()

        connectResult |> should be (ofCase <@ Connected @>)

        let! serverConnections = BrokerContainer.getConnections broker

        serverConnections
        |> should
            contain
            { AuthMechanism = "PLAIN"
              Username = username
              State = ConnectionState.Running }
    }

[<Theory(Timeout = 30_000)>]
[<MemberData(nameof allSupportedVersions)>]
let ``Connect with invalid credentials`` brokerVersion =
    task {
        let username = "invalid-user"
        let password = "invalid-password"

        use! broker = BrokerContainer.start brokerVersion

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ]
                AuthenticationStrategy = PlainText(username, password) }

        let client = AmqpClient(connectionConfiguration)

        let! connectResult = client.Connect()

        connectResult |> should be (ofCase <@ ConnectionFailed @>)
    }
    :> System.Threading.Tasks.Task

[<Fact>]
let ``Remote connection refused`` () =
    task {
        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ IPEndPoint(IPAddress.Parse("127.0.0.1"), 1) ] }

        let client = AmqpClient(connectionConfiguration)

        let! result = client.Connect()

        let expectedFailures =
            Map.ofList [ ("127.0.0.1:1", ConnectResult.ConnectionFailed ConnectionFailureReason.ConnectionRefused) ]

        result |> should equal (ConnectionFailed expectedFailures)
    }

[<Fact>]
let ``Connection timeout`` () =
    task {
        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ IPEndPoint(IPAddress.Parse("10.0.0.0"), 5672) ]
                ConnectTimeout = TimeSpan.FromSeconds(1L) }

        let client = AmqpClient(connectionConfiguration)

        let! result = client.Connect()

        let expectedFailures =
            Map.ofList [ ("10.0.0.0:5672", ConnectResult.ConnectionFailed ConnectionFailureReason.Timeout) ]

        result |> should equal (ConnectionFailed expectedFailures)
    }

[<Theory>]
[<MemberData(nameof allSupportedVersions)>]
let ``Disconnect`` brokerVersion =
    task {
        use! broker = BrokerContainer.start brokerVersion

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ] }

        let client = AmqpClient(connectionConfiguration)
        let! connectResult = client.Connect()

        // Assert that the connection was established, otherwise we're not actually testing anything...
        connectResult |> should be (ofCase <@ ConnectionResult.Connected @>)

        let! serverConnections = BrokerContainer.getConnections broker

        serverConnections
        |> should
            contain
            { AuthMechanism = "PLAIN"
              Username = "guest"
              State = ConnectionState.Running }

        client.Disconnect()

        // Assert that there are no active connections on the broker...
        let! connections = BrokerContainer.getConnections broker
        connections |> should be Empty
    }

[<Theory>]
[<MemberData(nameof allSupportedVersions)>]
let ``Disconnect and reconnect`` brokerVersion =
    task {
        use! broker = BrokerContainer.start brokerVersion

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ] }

        let client = AmqpClient(connectionConfiguration)
        let! connectResult = client.Connect()

        // Assert that the connection was established...
        connectResult |> should be (ofCase <@ ConnectionResult.Connected @>)
        let! serverConnections = BrokerContainer.getConnections broker

        serverConnections
        |> should
            contain
            { AuthMechanism = "PLAIN"
              Username = "guest"
              State = ConnectionState.Running }

        client.Disconnect()

        // Assert that there are no active connections on the broker...
        let! serverConnections = BrokerContainer.getConnections broker
        serverConnections |> should be Empty

        // Try to reconnect...
        let! connectResult = client.Connect()
        connectResult |> should be (ofCase <@ ConnectionResult.Connected @>)
        let! serverConnections = BrokerContainer.getConnections broker

        serverConnections
        |> should
            contain
            { AuthMechanism = "PLAIN"
              Username = "guest"
              State = ConnectionState.Running }
    }

[<Theory>]
[<MemberData(nameof allSupportedVersions)>]
let ``Two clients connected to the same broker`` brokerVersion =
    task {
        use! broker = BrokerContainer.start brokerVersion

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ] }

        let client1 = AmqpClient(connectionConfiguration)
        let client2 = AmqpClient(connectionConfiguration)

        let! _ = client1.Connect()
        let! _ = client2.Connect()

        let! serverConnections = BrokerContainer.getConnections broker
        serverConnections |> List.ofSeq |> should haveLength 2
    }

[<Theory>]
[<MemberData(nameof allSupportedVersions)>]
let ``Connect to virtual host`` brokerVersion =
    task {
        use! broker = BrokerContainer.start brokerVersion
        do! BrokerContainer.addVirtualHost "my-host" broker
        do! BrokerContainer.setPermissions "my-host" "guest" ".*" ".*" ".*" broker

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ]
                VirtualHost = "my-host" }

        let client = AmqpClient(connectionConfiguration)
        let! connectResult = client.Connect()

        // Assert that the connection was established...
        connectResult |> should be (ofCase <@ ConnectionResult.Connected @>)
        let! serverConnections = BrokerContainer.getConnections broker

        serverConnections
        |> should
            contain
            { AuthMechanism = "PLAIN"
              Username = "guest"
              State = ConnectionState.Running }
    }
