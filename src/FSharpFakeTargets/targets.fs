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
  let private AssemblyInfoFilePath = Path.Combine(RootDir, "AssemblyInfo.fs");

  type ConfigParams =
    {
      SolutionFile : FileIncludes
      MSBuildArtifacts : FileIncludes
      MSBuildReleaseArtifacts : FileIncludes
      MSBuildOutputDir : string
      NuspecFilePath : Option<string>
      AssemblyInfoFilePath : string
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
      AssemblyInfoFilePath = AssemblyInfoFilePath
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
            Version = GetAssemblyInformationalVersionString parameters.AssemblyInfoFilePath
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

  let private _RootAssemblyInfoVersioningTargets parameters =
    let versionAttributeName = "AssemblyInformationalVersion"
    let assemblyInfoFile = parameters.AssemblyInfoFilePath

    let _IncrementAssemblyInfo incrFn =
      let currentSemVer =
        match (AssemblyInfoFile.GetAttributeValue versionAttributeName parameters.AssemblyInfoFilePath) with
        | Some v -> v.Trim [|'"'|] // This util returns the string with actual " characters around it, so we have to strip them.
        | None _ ->
          let errorMessage = sprintf "Error: missing attribute `%s` in %s" versionAttributeName assemblyInfoFile
          traceError errorMessage
          exit 1

      let nextSemVer = incrFn currentSemVer
      let nextFullVer = (CoerceStringToFourVersion nextSemVer).ToString()

      AssemblyInfoFile.UpdateAttributes parameters.AssemblyInfoFilePath
        [|
            AssemblyInfoFile.Attribute.Version nextFullVer ;
            AssemblyInfoFile.Attribute.FileVersion nextFullVer ;
            AssemblyInfoFile.Attribute.InformationalVersion nextSemVer ;
        |]

    let _IncrementPatchTarget parameters =
      _CreateTarget "IncrementPatch:RootAssemblyInfo" parameters (fun _ ->
          _IncrementAssemblyInfo datNET.Version.IncrPatch
      )

    let _IncrementMinorTarget parameters =
      _CreateTarget "IncrementMinor:RootAssemblyInfo" parameters (fun _ ->
          _IncrementAssemblyInfo datNET.Version.IncrMinor
      )

    let _IncrementMajorTarget parameters =
      _CreateTarget "IncrementMajor:RootAssemblyInfo" parameters (fun _ ->
          _IncrementAssemblyInfo datNET.Version.IncrMajor
      )

    parameters
    |> _IncrementPatchTarget
    |> _IncrementMinorTarget
    |> _IncrementMajorTarget

  let Initialize setParams =
    let parameters = ConfigDefaults() |> setParams

    parameters
        |> _MSBuildTarget
        |> _CleanTarget
        |> _PackageTarget
        |> _PublishTarget
        |> _RootAssemblyInfoVersioningTargets
