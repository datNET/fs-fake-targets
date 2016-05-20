#r @"./._fake/packages/FAKE/tools/FakeLib.dll"

#r @"./._fake/packages/FSharp.FakeTargets/tools/FSharpFakeTargets.dll"
(*
  The commented line below is for local testing purposes. Make sure to

  1. Comment out the line above
  2. Uncommeht the line below
  2. Build this solution in VS

  After having done this, you should be able to try out whatever work you have
  done on the source code for FSharpFakeTargets without having to release or
  anything like that.
*)
//#r @"./src/FSharpFakeTargets/bin/Debug/FSharpFakeTargets.dll"

#load @"./._fake/loader.fsx"

open Fake
open RestorePackageHelper
open datNET.Fake.Config

let private _OverrideConfig (parameters : datNET.Targets.ConfigParams) =
      { parameters with
          Project = Release.Project
          Authors = Release.Authors
          Description = Release.Description
          WorkingDir = Release.WorkingDir
          OutputPath = Release.OutputPath
          Publish = true
          AccessKey = Nuget.ApiKey
      }

datNET.Targets.Initialize _OverrideConfig

Target "RestorePackages" (fun _ ->
  Source.SolutionFile
  |> Seq.head
  |> RestoreMSSolutionPackages (fun p ->
      { p with
          Sources = [ "https://nuget.org/api/v2" ]
          OutputPath = "packages"
          Retries = 4 })
)

"MSBuild"           <== [ "Clean"; "RestorePackages" ]
"Test"              <== [ "MSBuild" ]
"Package"           <== [ "MSBuild" ]
"Publish"           <== [ "Package" ]

RunTargetOrDefault "MSBuild"
