﻿namespace datNET

module Targets =
  open Fake
  open Fake.FileSystem
  open System.IO
  open Fake.FileSystemHelper
  open Fake.NuGetHelper

  let private RootDir = Directory.GetCurrentDirectory()

  type ConfigParams =
    {
      SolutionFile : FileIncludes
      MSBuildArtifacts : FileIncludes
      MSBuildReleaseArtifacts : FileIncludes
      MSBuildOutputDir : string
      NuspecFilePath : string
      NuGetParams : NuGetParams
    }

  let ConfigDefaults() =
    {
      SolutionFile = !! (Path.Combine(RootDir, "*.sln"))
      MSBuildArtifacts = !! "src/**/bin/**/*.*" ++ "src/**/obj/**/*.*"
      MSBuildReleaseArtifacts = !! "**/bin/Release/*"
      MSBuildOutputDir = "bin"
      NuspecFilePath = FindFirstMatchingFile "*.nuspec" "."
      NuGetParams = NuGetDefaults()
    }

  let private _CreateTarget targetName parameters targetFunc =
    Target targetName targetFunc
    parameters

  let private _MSBuildTarget parameters =
    _CreateTarget "MSBuild" parameters (fun _ ->
        parameters.SolutionFile
            |> MSBuildRelease null "Build"
            |> ignore

        Copy parameters.MSBuildOutputDir parameters.MSBuildReleaseArtifacts
    )

  let private _CleanTarget parameters =
    _CreateTarget "Clean" parameters (fun _ ->
        DeleteFiles parameters.MSBuildArtifacts
        CleanDir parameters.MSBuildOutputDir
    )

  let private _PackageTarget parameters =
    _CreateTarget "Package" parameters (fun _ ->
        parameters.NuspecFilePath
            |> NuGetPack (fun nugetParams -> parameters.NuGetParams)
    )

  let Initialize setParams =
    let parameters = ConfigDefaults() |> setParams

    parameters
        |> _MSBuildTarget
        |> _CleanTarget
        |> _PackageTarget
