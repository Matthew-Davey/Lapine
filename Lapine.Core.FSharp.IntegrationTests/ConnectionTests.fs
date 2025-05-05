module ``Connection Tests``

open System.Net
open System.Threading
open AmqpTypes
open Amqp
open FsUnit
open FsUnit.CustomMatchers
open Xunit
open AmqpClient

let allSupportedVersions () = seq {
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
[<MemberData(nameof(allSupportedVersions))>]
let ``Connect as guest`` brokerVersion = task {
    use! broker = BrokerContainer.start brokerVersion
    let connectionConfiguration = BrokerContainer.getConnectionConfiguration broker
    
    let client = AmqpClient(connectionConfiguration)

    let! connectResult = client.Connect(CancellationToken.None)

    connectResult |> should be (ofCase <@ Connected @>)
}

[<Theory>]
[<MemberData(nameof(allSupportedVersions))>]
let ``Connect as user`` brokerVersion = task {
    // TODO: Use randomized inputs...
    let username = "foo"
    let password = "bar"
    
    use! broker = BrokerContainer.start brokerVersion
    do! BrokerContainer.addUser username password broker
    do! BrokerContainer.setPermissions "/" username ".*" ".*" ".*" broker
    
    let connectionConfiguration =
        { BrokerContainer.getConnectionConfiguration broker with
              AuthenticationStrategy = PlainText(username, password) }
    
    let client = AmqpClient(connectionConfiguration)
    
    let! connectResult = client.Connect(CancellationToken.None)
    
    connectResult |> should be (ofCase <@ Connected @>)
}

[<Theory>]
[<MemberData(nameof(allSupportedVersions))>]
let ``Connect with invalid credentials`` brokerVersion = task {
    let username = "invalid-user"
    let password = "invalid-password"
    
    use! broker = BrokerContainer.start brokerVersion
    
    let connectionConfiguration =
        { BrokerContainer.getConnectionConfiguration broker with
              AuthenticationStrategy = PlainText(username, password) }
        
    let client = AmqpClient(connectionConfiguration)
    
    let! connectResult = client.Connect(CancellationToken.None)
    
    connectResult |> should be (ofCase <@ ConnectionFailed @>)
}

[<Fact>]
let ``Remote connection refused`` () = task {
    let connectionConfiguration =
        { ConnectionConfiguration.default' with
              EndPoints = [ IPEndPoint(IPAddress.Parse("127.0.0.1"), 1) ] }
    
    let client = AmqpClient(connectionConfiguration)

    let! connectResult = client.Connect(CancellationToken.None)
    
    connectResult |> should be (ofCase <@ ConnectionFailed @>)
}
