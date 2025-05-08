#! /bin/bash

dotnet fantomas ./Lapine.Core.FSharp/Buffer.fs
dotnet fantomas ./Lapine.Core.FSharp/AmqpTypes.fs
dotnet fantomas ./Lapine.Core.FSharp/Amqp.fs
dotnet fantomas ./Lapine.Core.FSharp/Agent.fs
dotnet fantomas ./Lapine.Core.FSharp/AgentMessages.fs
dotnet fantomas ./Lapine.Core.FSharp/TcpConnectionAgent.fs
dotnet fantomas ./Lapine.Core.FSharp/AmqpConnectionAgent.fs
dotnet fantomas ./Lapine.Core.FSharp/HandshakeAgent.fs
dotnet fantomas ./Lapine.Core.FSharp/HeartbeatAgent.fs
dotnet fantomas ./Lapine.Core.FSharp/ConnectionSupervisor.fs
dotnet fantomas ./Lapine.Core.FSharp/ChannelAgent.fs
dotnet fantomas ./Lapine.Core.FSharp/AmqpClient.fs

dotnet fantomas ./Lapine.Core.FSharp.IntegrationTests/BrokerContainer.fs
dotnet fantomas ./Lapine.Core.FSharp.IntegrationTests/ConnectionTests.fs
dotnet fantomas ./Lapine.Core.FSharp.IntegrationTests/ChannelTests.fs