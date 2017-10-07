#! /bin/sh
set -e

cd ._fake
./paket.sh restore --fail-on-checks
cd ..

./._fake/packages/FAKE/tools/FAKE.exe build.fsx $@
