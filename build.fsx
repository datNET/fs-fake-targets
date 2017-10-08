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
      NuspecFilePath = None
  }
)

Target "Paket:Pack" (fun _ ->
  Paket.Pack (fun p ->
    { p with
        MinimumFromLockFile = true
        OutputPath          = Release.OutputPath
    }
  )
)

Target "Deprecate:UsePaket" (fun _ ->
  failwith <| @"

  Packaging via NuGet will not work in repositories where all dependencies are
  managed via Paket.

  Please use the `Paket:Pack` FAKE target instead, or use paket directly
  instead.

  "
)
"Package:Project" <== ["Deprecate:UsePaket"]
"Package:Nuspec"  <== ["Deprecate:UsePaket"]

"MSBuild"    <== ["Clean"]
"Paket:Pack" <== ["MSBuild"]
"Publish"    <== ["Paket:Pack"]

RunTargetOrDefault "MSBuild"
