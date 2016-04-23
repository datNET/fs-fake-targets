namespace datNET

module Targets =
  open Fake
  open Fake.FileSystem
  open System.IO
  open System
  open Fake.FileSystemHelper
  open Fake.NuGetHelper

  let private RootDir = Directory.GetCurrentDirectory()

  type ConfigParams =
    {
      SolutionFile : FileIncludes
      MSBuildArtifacts : FileIncludes
      MSBuildReleaseArtifacts : FileIncludes
      MSBuildOutputDir : string
      NuspecFilePath : Option<string>
      Version : string
      Project : string
      Authors : string list
      Description : string
      OutputPath : string
      WorkingDir : string
      Publish : bool
      AccessKey : string
    }

  let ConfigDefaults() =
    {
      SolutionFile = !! (Path.Combine(RootDir, "*.sln"))
      MSBuildArtifacts = !! "src/**/bin/**/*.*" ++ "src/**/obj/**/*.*"
      MSBuildReleaseArtifacts = !! "**/bin/Release/*"
      MSBuildOutputDir = "bin"
      NuspecFilePath = TryFindFirstMatchingFile "*.nuspec" "."
      Version = String.Empty
      Project = String.Empty
      Authors = List.Empty
      Description = String.Empty
      OutputPath = String.Empty
      WorkingDir = String.Empty
      Publish = false
      AccessKey = String.Empty
    }

  let private _EnsureNuspecFileExists filePath =
    match filePath with
    | Some x -> x
    | None -> raise (FileNotFoundException("Could not find the nuspec file"))

  let private _CreateTarget targetName parameters targetFunc =
    Target targetName targetFunc
    parameters

  let private _CreateNuGetParams parameters =
    (fun (nugetParams : NuGetParams) ->
        { nugetParams with
            Version = parameters.Version
            Project = parameters.Project
            Authors = parameters.Authors
            Description = parameters.Description
            OutputPath = parameters.OutputPath
            WorkingDir = parameters.WorkingDir
            Publish = parameters.Publish
            AccessKey = parameters.AccessKey
        })

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
            |> _EnsureNuspecFileExists
            |> NuGetPack (_CreateNuGetParams parameters)
    )

  let private _PublishTarget parameters =
    _CreateTarget "Publish" parameters (fun _ ->
        NuGetPublish (_CreateNuGetParams parameters)
    )

  let Initialize setParams =
    let parameters = ConfigDefaults() |> setParams

    parameters
        |> _MSBuildTarget
        |> _CleanTarget
        |> _PackageTarget
        |> _PublishTarget
