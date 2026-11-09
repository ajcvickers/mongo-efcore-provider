#!/bin/bash
# usage: run_linux11.sh <label> <NativeOnly|Default>   -- EF11 (xUnit v3 test host run directly, as CI does)
export DOTNET_ROOT=$HOME/dotnet11 PATH=$HOME/dotnet11:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="${SRC:-$(cd "$HERE/../../../.." && pwd)}"       # the checkout to test (WSL path, e.g. /mnt/d/code/...)
W="${OUT_ROOT:-$HOME/work/spec-out}"; mkdir -p "$W"
cd "${CLONE:-$HOME/work/ef}" || exit 1
git -c safe.directory='*' checkout -q -- .
git -c safe.directory='*' fetch -q "$SRC" ${BRANCH:-release} && git -c safe.directory='*' reset -q --hard FETCH_HEAD
git log --oneline -1
P=tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj
dotnet build $P -c "Debug EF11" -o $HOME/work/ef11out -v q 2>&1 | grep -E "Error\(s\)| error " | sort -u | head -3 | cut -c1-200
export TZ=UTC MONGODB_URI="mongodb://localhost:27099/?directConnection=true" ATLAS_URI=Disabled
[ "$2" = "NativeOnly" ] && export MONGODB_EF_QUERY_MODE=NativeOnly
timeout 1500 $HOME/work/ef11out/MongoDB.EntityFrameworkCore.SpecificationTests -noColor -result-trx $W/$1.trx > $W/$1.log 2>&1
grep "Total:" $W/$1.log | tail -1 | cut -c1-150
