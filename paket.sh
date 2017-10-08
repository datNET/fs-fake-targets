#! /bin/sh
set -e

if [ ! -f .paket/paket.exe ]; then
  .paket/paket.bootstrapper.exe
fi

.paket/paket.exe $@
