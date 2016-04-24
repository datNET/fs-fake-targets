namespace datNET.Fake.Config

#r @"./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.EnvironmentHelper
open System.IO

module Common =
  let RootDir = Directory.GetCurrentDirectory()

module Source =
  open Common

  let SolutionFile = !! (Path.Combine(RootDir, "*.sln"))

module Build =
  let TestAssemblies = !! "tests/**/*.Tests.dll" -- "**/obj/**/*.Tests.dll"
  let DotNetVersion = "4.5"
  let MSBuildArtifacts = !! "**/bin/**/*.*" ++ "**/obj/**/*.*"

module Nuget =
  let ApiEnvVar = "DAT_NET_NUGET_API_KEY"
  let ApiKey = environVar ApiEnvVar
  let PackageDirName = "nupkgs"

module Release =
  let Items = !! "**/bin/Release/*"
  let Nuspec = "FsharpFakeTargets.nuspec"

  let Version = "0.4.1"
  let Project = "FSharp.FakeTargets"
  let Authors = [ "Andrew Seward"; "Mathew Glodack" ]
  let Description = "Basic FAKE targets to get your project up and running ASAP"
  let WorkingDir = "bin"
  let OutputPath = WorkingDir
