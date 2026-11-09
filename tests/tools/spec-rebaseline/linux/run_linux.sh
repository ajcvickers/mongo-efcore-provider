#!/bin/bash
# usage: run_linux.sh <label> <config e.g. "Debug EF10"> <NativeOnly|Default> [filter]
export DOTNET_ROOT=$HOME/dotnet10 PATH=$HOME/dotnet10:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="${SRC:-$(cd "$HERE/../../../.." && pwd)}"       # the checkout to test (WSL path, e.g. /mnt/d/code/...)
W="${OUT_ROOT:-$HOME/work/spec-out}"; mkdir -p "$W"
cd "${CLONE:-$HOME/work/ef}" || exit 1
git -c safe.directory='*' checkout -q -- . 
git -c safe.directory='*' fetch -q "$SRC" ${BRANCH:-release} && git -c safe.directory='*' reset -q --hard FETCH_HEAD
git log --oneline -1
P=tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj
dotnet build $P -c "$2" -v q 2>&1 | grep -E "Error\(s\)| error " | sort -u | head -3 | cut -c1-200
export TZ=UTC MONGODB_URI="mongodb://localhost:27099/?directConnection=true" ATLAS_URI=Disabled
[ "$3" = "NativeOnly" ] && export MONGODB_EF_QUERY_MODE=NativeOnly
F=""; [ -n "$4" ] && F="--filter $4"
timeout 1500 dotnet test $P -c "$2" --no-build $F --logger "trx;LogFileName=$W/$1.trx" 2>&1 | grep -E "^(Passed|Failed)!" | cut -c1-150
