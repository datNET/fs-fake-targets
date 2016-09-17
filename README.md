# fs-fake-targets [![Build status](https://ci.appveyor.com/api/projects/status/l6dj0i2chw5denwv/branch/master?svg=true)](https://ci.appveyor.com/project/datNET/fs-fake-targets/branch/master) [![NuGet version](https://badge.fury.io/nu/FSharp.FakeTargets.svg)](https://badge.fury.io/nu/FSharp.FakeTargets)

FAKE targets for getting projects off the ground ASAP

### Installation

```
Install-Package FSharp.FakeTargets
```

### Usage

Your `build.fsx` file might look like the following if you are using `NuGet` as your package manager.

```fsx
#r @"packages/FAKE/tools/FakeLib.dll"
#r @"packages/FSharp.FakeTargets/tools/FSharpFakeTargets.dll"

open Fake

let private _overrideConfig (parameters : datNET.Targets.ConfigParams) =
  { parameters with
      Project     = "PigsCanFly"
      Authors     = [ "Tom"; "Jerry" ]
      Description = "A library to let your pigs fly."
      WorkingDir  = "bin"
      OutputPath  = "bin"
      Publish     = true
      AccessKey   = "Your secret nuget api key here"
  }

datNET.Targets.initialize _overrideConfig

Target "RestorePackages" (fun _ ->
  "PigCanFly.sln"
  |> Seq.head
  |> RestoreMSSolutionPackages (fun p ->
      { p with
          Sources    = ["https://nuget.org/api/v2"]
          OutputPath = "packages"
          Retries    = 4
      }
  )
)

"MSBuild" <== ["Clean"; "RestorePackages"]
"Test"    <== ["MSBuild"]
"Package" <== ["MSBuild"]
"Publish" <== ["Package"]

RunTargetOrDefault "MSBuild"
```
