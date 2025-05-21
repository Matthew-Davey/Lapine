module ``Channel Tests``

open FsUnit
open Xunit
open Amqp
open AmqpClient

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
let ``Open a channel`` brokerVersion =
    task {
        use! broker = BrokerContainer.start brokerVersion

        let connectionConfiguration =
            { ConnectionConfiguration.default' with
                EndPoints = [ BrokerContainer.endPoint broker ] }

        let client = AmqpClient(connectionConfiguration)

        let! _ = client.Connect()

        let! channel = client.OpenChannel()

        let! channels = BrokerContainer.getChannels broker
        channels |> List.ofSeq |> should haveLength 1
    }
