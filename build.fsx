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

datNET.Targets.initialize _OverrideConfig

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
