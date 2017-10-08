#load @"._fake/loader.fsx"

open Fake
open RestorePackageHelper
open datNET.Fake.Config

datNET.Targets.initialize (fun parameters ->
  { parameters with
      Project     = Release.Project
      Authors     = Release.Authors
      Description = Release.Description
      WorkingDir  = Release.WorkingDir
      OutputPath  = Release.OutputPath
      Publish     = true
      AccessKey   = Nuget.ApiKey
      ProjectFilePath = Some("src/FSharpFakeTargets/FSharp.FakeTargets.fsproj")
      NuspecFilePath = Some(Release.Nuspec)
  }
)

Target "RestorePackages" (fun _ ->
  Source.SolutionFile
  |> Seq.head
  |> RestoreMSSolutionPackages (fun p ->
      { p with
          Sources    = ["https://nuget.org/api/v2"]
          OutputPath = "packages"
          Retries    = 4
      }
  )
)

"MSBuild"               <== ["Clean"; "RestorePackages"]
"Test"                  <== ["MSBuild"]
"Package:Nuspec"        <== ["MSBuild"]
"Package:Project"       <== ["MSBuild"]
"Publish"               <== ["Package:Project"]

RunTargetOrDefault "MSBuild"
