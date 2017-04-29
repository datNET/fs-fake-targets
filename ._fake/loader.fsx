#r    @"packages/FAKE/tools/FakeLib.dll"
#r    @"packages/FSharp.FakeTargets/tools/FSharp.FakeTargets.dll"
(*
  The commented line below is for local testing purposes. Make sure to

  1. Comment out the line above
  2. Uncommeht the line below
  2. Build this solution in VS

  After having done this, you should be able to try out whatever work you have
  done on the source code for FSharpFakeTargets without having to release or
  anything like that.
*)
// #r    @"../src/FSharpFakeTargets/bin/Debug/FSharpFakeTargets.dll"

#load @"config.fsx"
