@echo off

set fake_args=%*

call "paket.bat" "restore"

"packages\build\FAKE\tools\FAKE.exe" "build.fsx" %fake_args%
