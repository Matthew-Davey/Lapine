#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli //"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet

Target.create "Clean" (fun _ ->
    DotNet.exec id "clean" ""
    |> ignore
)

Target.create "Build" (fun _ ->
    DotNet.build id ""
)

Target.create "Test" (fun _ ->
    DotNet.test id "Lapine.Core.Tests/Lapine.Core.Tests.csproj"
)

Target.create "IntegrationTest" (fun _ ->
    DotNet.test id "Lapine.Core.IntegrationTests/Lapine.Core.IntegrationTests.csproj"
)

"Clean"
  ==> "Build"
  ==> "Test"

Target.runOrDefault "Test"
