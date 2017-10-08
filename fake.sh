#! /bin/sh
set -e

./paket.sh restore --fail-on-checks

./packages/build/FAKE/tools/FAKE.exe build.fsx $@
