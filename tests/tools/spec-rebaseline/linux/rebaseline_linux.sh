#!/bin/bash
# Normal + harvest runs of a few classes on Linux (TZ=UTC), default query mode, for EF8, EF9, EF10 and EF11.
# usage: BRANCH=<branch> bash rebaseline_linux.sh <outdir name under $OUT_ROOT> "<dotnet filter>" "<ns.Class>,<ns.Class>..."
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="${SRC:-$(cd "$HERE/../../../.." && pwd)}"       # the checkout to test (WSL path, e.g. /mnt/d/code/...)
W="${OUT_ROOT:-$HOME/work/spec-out}"; mkdir -p "$W"
HARVEST="$HERE/../SpecFails.harvest.cs"
OUT=$W/$1; FILTER=$2; CLASSES=$3
mkdir -p "$OUT"
cd "${CLONE:-$HOME/work/ef}" || exit 1
git -c safe.directory='*' fetch -q "$SRC" ${BRANCH:-release} && git -c safe.directory='*' reset -q --hard FETCH_HEAD
git log --oneline -1
P=tests/MongoDB.EntityFrameworkCore.SpecificationTests/MongoDB.EntityFrameworkCore.SpecificationTests.csproj
SF=tests/MongoDB.EntityFrameworkCore.SpecificationTests/Utilities/SpecFails.cs
export TZ=UTC MONGODB_URI="mongodb://localhost:27099/?directConnection=true" ATLAS_URI=Disabled
unset MONGODB_EF_QUERY_MODE

run_dotnet() { # <ver> <phase normal|harvest>
  export DOTNET_ROOT=$HOME/dotnet10 PATH=$HOME/dotnet10:$PATH
  dotnet build $P -c "Debug EF$1" -v q 2>&1 | grep -E "Error\(s\)| error " | sort -u | head -3 | cut -c1-200
  if [ "$2" = harvest ]; then
    rm -f "$OUT/harvest-ef$1-default.tsv"
    SPEC_FAILS_HARVEST="$OUT/harvest-ef$1-default.tsv" timeout 1200 dotnet test $P -c "Debug EF$1" --no-build --filter "$FILTER" 2>&1 | grep -E "^(Passed|Failed)!" | cut -c1-110
  else
    timeout 1200 dotnet test $P -c "Debug EF$1" --no-build --filter "$FILTER" --logger "trx;LogFileName=$OUT/ef$1-default.trx" 2>&1 | grep -E "^(Passed|Failed)!" | cut -c1-110
  fi
}
run_ef11() { # <phase>
  export DOTNET_ROOT=$HOME/dotnet11 PATH=$HOME/dotnet11:$PATH
  dotnet build $P -c "Debug EF11" -o $HOME/work/ef11out -v q 2>&1 | grep -E "Error\(s\)| error " | sort -u | head -3 | cut -c1-200
  ARGS=""; for c in ${CLASSES//,/ }; do ARGS="$ARGS -class $c"; done
  if [ "$1" = harvest ]; then
    rm -f "$OUT/harvest-ef11-default.tsv"
    SPEC_FAILS_HARVEST="$OUT/harvest-ef11-default.tsv" timeout 1200 $HOME/work/ef11out/MongoDB.EntityFrameworkCore.SpecificationTests -noColor $ARGS 2>&1 | grep "Total:" | tail -1 | cut -c1-110
  else
    timeout 1200 $HOME/work/ef11out/MongoDB.EntityFrameworkCore.SpecificationTests -noColor $ARGS -result-trx "$OUT/ef11-default.trx" 2>&1 | grep "Total:" | tail -1 | cut -c1-110
  fi
}

echo "##### normal runs"
for v in 8 9 10; do echo "EF$v:"; run_dotnet $v normal; done
echo "EF11:"; run_ef11 normal
if [ -n "$NOHARVEST" ]; then echo done; exit 0; fi
echo "##### harvest runs (temporary SpecFails)"
cp "$HARVEST" "$SF"
for v in 8 9 10; do echo "EF$v:"; run_dotnet $v harvest; done
echo "EF11:"; run_ef11 harvest
git -c safe.directory='*' checkout -q -- "$SF"
echo done
