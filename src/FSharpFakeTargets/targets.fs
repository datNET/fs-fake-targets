namespace datNET

module Targets =
  open datNET.Version
  open datNET.AssemblyInfo
  open Fake
  open Fake.FileSystem
  open Fake.FileSystemHelper
  open Fake.NuGetHelper
  open System
  open System.IO

  let private RootDir = Directory.GetCurrentDirectory()

  // TODO: Unhardcode this ASAP
  let private _AssemblyInfoFilePath = System.IO.Path.Combine(RootDir, "AssemblyInfo.fs");

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

  let private _IncrementAssemblyInfo incrFn =
    let currentSemVer = GetAssemblyInformationalVersionString _AssemblyInfoFilePath
    let nextSemVer = incrFn currentSemVer
    let nextFullVer = (CoerceStringToFourVersion nextSemVer).ToString()
    let aiContents = File.ReadAllText(_AssemblyInfoFilePath)

    let pipeShim setVersion value fileContents =
      setVersion fileContents value
      |> fun (strSeq: seq<string>) -> String.Join(Environment.NewLine, strSeq)

    aiContents
    |> pipeShim SetAssemblyVersion nextFullVer
    |> pipeShim SetAssemblyFileVersion nextFullVer
    |> pipeShim SetAssemblyInformationalVersion nextSemVer
    |> fun str -> File.WriteAllText(_AssemblyInfoFilePath, str)

  let private _IncrementPatchTarget parameters =
    _CreateTarget "IncrementPatch" parameters (fun _ ->
        _IncrementAssemblyInfo datNET.Version.IncrPatch
    )

  let private _IncrementMinorTarget parameters =
    _CreateTarget "IncrementMinor" parameters (fun _ ->
        _IncrementAssemblyInfo datNET.Version.IncrMinor
    )

  let private _IncrementMajorTarget parameters =
    _CreateTarget "IncrementMajor" parameters (fun _ ->
        _IncrementAssemblyInfo datNET.Version.IncrMajor
    )

  let Initialize setParams =
    let parameters = ConfigDefaults() |> setParams

    parameters
        |> _MSBuildTarget
        |> _CleanTarget
        |> _PackageTarget
        |> _PublishTarget
        |> _IncrementPatchTarget
        |> _IncrementMinorTarget
        |> _IncrementMajorTarget
