namespace Lapine.AmqpClient

module ``Channel Tests`` =

    open FsUnit
    open Xunit
    open Lapine.AmqpClient

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
                { ConnectionConfiguration.Default with
                    EndPoints = [ BrokerContainer.endPoint broker ] }

            let client = AmqpClient(connectionConfiguration)

            let! _ = client.Connect()

            let! channel = client.OpenChannel()

            let! channels = BrokerContainer.getChannels broker
            channels |> List.ofSeq |> should haveLength 1
        }

    [<Theory>]
    [<MemberData(nameof allSupportedVersions)>]
    let ``Close a channel`` brokerVersion =
        task {
            use! broker = BrokerContainer.start brokerVersion
            
            let connectionConfiguration =
                { ConnectionConfiguration.Default with
                    EndPoints = [ BrokerContainer.endPoint broker ] }
                
            let client = AmqpClient(connectionConfiguration)
            
            let! _ = client.Connect()
            
            let! channel = client.OpenChannel()
            
            let! channels = BrokerContainer.getChannels broker
            channels |> List.ofSeq |> should haveLength 1
            
            channel.Close() |> ignore
            
            let! channels = BrokerContainer.getChannels broker
            channels |> List.ofSeq |> should haveLength 0
        }
    
    [<Theory>]
    [<MemberData(nameof allSupportedVersions)>]
    let ``Close and re-open a channel`` brokerVersion =
        task {
            use! broker = BrokerContainer.start brokerVersion
            
            let connectionConfiguration =
                { ConnectionConfiguration.Default with
                    EndPoints = [ BrokerContainer.endPoint broker ] }
                
            let client = AmqpClient(connectionConfiguration)
            
            let! _ = client.Connect()
            
            let! channel = client.OpenChannel()
            
            let! channels = BrokerContainer.getChannels broker
            channels |> List.ofSeq |> should haveLength 1
            
            channel.Close() |> ignore
            
            let! channels = BrokerContainer.getChannels broker
            channels |> List.ofSeq |> should haveLength 0
            
            let _ = channel.Open()
            
            let! channels = BrokerContainer.getChannels broker
            channels |> List.ofSeq |> should haveLength 1
        }