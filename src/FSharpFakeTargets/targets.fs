namespace datNET

module Targets =
  open datNET.Validations
  open Fake
  open Fake.FileSystem
  open Fake.FileSystemHelper
  open Fake.NuGetHelper
  open System
  open System.IO

  let private _rootDir = Directory.GetCurrentDirectory()
  // FIXME: This is a little presumptious
  let private _assemblyInfoFilePath = Path.Combine(_rootDir, "AssemblyInfo.fs");
  let private _infoAttrName = "AssemblyInformationalVersion"

  type TestTool = NUnit | XUnit

  type ConfigParams =
    {
      SolutionFile:            string seq
      MSBuildArtifacts:        string seq
      MSBuildReleaseArtifacts: string seq
      MSBuildOutputDir:        string
      TestAssemblies:          string seq
      DotNetVersion:           string
      NuspecFilePath:          string option
      AssemblyInfoFilePaths:   string seq
      Project:                 string
      Authors:                 string list
      Description:             string
      OutputPath:              string
      WorkingDir:              string
      Publish:                 bool
      AccessKey:               string
      PublishUrl:              string
      Properties:              (string * string) list
      ProjectFilePath:         string option
      TestTool:                TestTool
    }

  let ConfigDefaults () =
    {
      SolutionFile            = !! (Path.Combine(_rootDir, "*.sln"))
      MSBuildArtifacts        = !! "src/**/bin/**/*.*" ++ "src/**/obj/**/*.*"
      MSBuildReleaseArtifacts = !! "**/bin/Release/*"
      MSBuildOutputDir        = "bin"
      TestAssemblies          = !! "tests/**/*.Tests.dll" -- "**/obj/**/*.Tests.dll"
      DotNetVersion           = "4.0"
      NuspecFilePath          = TryFindFirstMatchingFile "*.nuspec" "."
      AssemblyInfoFilePaths   = [ _assemblyInfoFilePath ]
      Project                 = String.Empty
      Authors                 = List.Empty
      Description             = String.Empty
      OutputPath              = String.Empty
      WorkingDir              = String.Empty
      Publish                 = false
      AccessKey               = String.Empty
      PublishUrl              = String.Empty
      Properties              = [ ("Configuration", "Release") ]
      ProjectFilePath         = None
      TestTool                = NUnit
    }

  let private _readVersionString filePath =
      match (AssemblyInfoFile.GetAttributeValue _infoAttrName filePath) with
      | Some verStr -> verStr.Trim[| '"' |]
      | None ->
          let errorMessage = sprintf "Error: missing attribute `%s` in %s" _infoAttrName filePath
          traceError errorMessage
          exit 1

  let private _target name func parameters =
    Target name (fun _ -> func parameters)
    parameters

  let private _wrap message wrapper =
    List.concat [ wrapper; [message]; wrapper ]

  let private _displayWarningMessage message =
    let emptyLines = [ ""; "" ]
    traceError ((_wrap message emptyLines) |> String.concat Environment.NewLine)

  let private _createNuGetParams configParams nugetParams =
    let version =
      configParams.AssemblyInfoFilePaths
      |> Seq.head
      |> _readVersionString

    { nugetParams with
        Version     = version
        Project     = configParams.Project
        Authors     = configParams.Authors
        Description = configParams.Description
        OutputPath  = configParams.OutputPath
        WorkingDir  = configParams.WorkingDir
        Publish     = configParams.Publish
        PublishUrl  = configParams.PublishUrl
        AccessKey   = configParams.AccessKey
        Properties  = configParams.Properties
    }

  let private _msBuildTarget = _target "MSBuild" (fun parameters ->
    parameters.SolutionFile
    |> MSBuildRelease null "Build"
    |> ignore

    Copy parameters.MSBuildOutputDir parameters.MSBuildReleaseArtifacts
  )

  let private _cleanTarget = _target "Clean" (fun parameters ->
    DeleteFiles parameters.MSBuildArtifacts
    CleanDir parameters.MSBuildOutputDir
  )

  let private _nuniTestTarget = _target "Test" (fun parameters ->
    let { DotNetVersion = dotNET; TestAssemblies = tests } = parameters
    let run = Fake.NUnitSequential.NUnit (fun p ->
      { p with
          DisableShadowCopy = true
          Framework = dotNET
      }
    )

    run tests
  )

  let private _xunitTestTarget = _target "Test:XUnit" (fun parameters ->
    parameters.TestAssemblies
    |> Fake.Testing.XUnit.xUnit id
  )

  let private _packageFromProjectTarget = _target "Package:Project" (fun parameters ->
    parameters.ProjectFilePath
    |> EnsureConfigPropertyFileExists "Project file"
    |> NuGetPack (_createNuGetParams parameters)
  )

  let private _packFromNuspec parameters =
    parameters.NuspecFilePath
    |> EnsureConfigPropertyFileExists "Nuspec file"
    |> NuGetPack (_createNuGetParams parameters)

  let private _packageFromNuspecTarget = _target "Package:Nuspec" (fun parameters ->
    parameters
    |> _packFromNuspec
  )

  [<Obsolete("Please use `_packageFromNuspecTarget`.")>]
  let private _obsoletePackageNuspecTarget = _target "Package" (fun parameters ->
    _displayWarningMessage "Warning: `Package` target will be renamed to `Package:Nuspec` in the next breaking version change."
    parameters
    |> _packFromNuspec
  )

  let private _publishTarget = _target "Publish" (fun parameters ->
    parameters
    |> _createNuGetParams
    |> NuGetPublish
  )

  module VersionTargets =

    let private _createVersionAttributes semVer =
      let sysVer =
        semVer
        |> datNET.SemVer.toSystemVersion None
        |> sprintf "%O"

      [|
        AssemblyInfoFile.Attribute.Version              sysVer
        AssemblyInfoFile.Attribute.FileVersion          sysVer
        AssemblyInfoFile.Attribute.InformationalVersion (datNET.SemVer.stringify semVer)
      |]

    let private _map fn filePath =
      _readVersionString filePath
      |> datNET.SemVer.parse
      |> fn
      |> _createVersionAttributes
      |> AssemblyInfoFile.UpdateAttributes filePath

    let getRequiredBuildParam paramName =
      match (getBuildParam paramName) with
      | ""         -> invalidArg paramName "Missing required parameter"
      | paramValue -> Some paramValue

    let private _majorFn p =
      p.AssemblyInfoFilePaths
      |> Seq.iter (_map datNET.SemVer.incrMajor)

    let private _minorFn p =
      p.AssemblyInfoFilePaths
      |> Seq.iter (_map datNET.SemVer.incrMinor)

    let private _patchFn p =
      p.AssemblyInfoFilePaths
      |> Seq.iter (_map datNET.SemVer.incrPatch)

    let private _setPre value = datNET.SemVer.mapPre (fun _ -> value)

    let private _setPreFn p =
      let pre = getRequiredBuildParam "pre"
      p.AssemblyInfoFilePaths
      |> Seq.iter (_map (_setPre pre))

    let private _unsetPreFn p =
      p.AssemblyInfoFilePaths
      |> Seq.iter (_map (_setPre None))

    let create parameters =
      let tName = sprintf "Version:%s"
      parameters
      |> _target "Version" (fun p ->
           p.AssemblyInfoFilePaths
           |> Seq.iter (fun path ->
                tracefn "[%s]" path
                tracefn "Current version: %s" (_readVersionString path)
              )
         )
      |> _target (tName "Major") _majorFn
      |> _target (tName "Minor") _minorFn
      |> _target (tName "Patch") _patchFn
      |> _target (tName "SetPre") _setPreFn
      |> _target (tName "UnsetPre") _unsetPreFn
      |> _target (tName "SetMeta") (fun p ->
           let meta = getRequiredBuildParam "meta"
           let setMeta = datNET.SemVer.mapMeta (fun _ -> meta)

           p.AssemblyInfoFilePaths |> Seq.iter (_map setMeta)
         )
      |> _target (tName "UnsetMeta") (fun p ->
           let setMeta = datNET.SemVer.mapMeta (fun _ -> None)

           p.AssemblyInfoFilePaths |> Seq.iter (_map setMeta)
         )

  let _testTarget parameters =
    let testTarget =
      match parameters.TestTool with
      | NUnit -> _nuniTestTarget
      | XUnit -> _xunitTestTarget

    testTarget parameters

  let initialize mapParams =
    let parameters = ConfigDefaults() |> mapParams

    parameters
    |> _msBuildTarget
    |> _cleanTarget
    |> _obsoletePackageNuspecTarget
    |> _packageFromProjectTarget
    |> _testTarget
    |> _publishTarget
    |> VersionTargets.create
