# Spec test re-baselining kit

Tools used to port the EF Core specification tests and to keep their `// Fails: <reason> <ticket>` overrides in step with the
provider (issues #35, #43, #44). Conventions are in `tests/MongoDB.EntityFrameworkCore.SpecificationTests/AGENTS.md`;
the inventory is `docs/failing-spec-tests.md`; `../count-spec-fails.py` refreshes its counts.

| Tool | Purpose |
| --- | --- |
| `skeleton.py <trx> <Class> <file.cs>` | Insert pass-through `=> base.X()` overrides for every test the base class has and the port class lacks (reads the `Check_all_tests_overridden` failure message from a TRX). Fix signatures by hand if the build complains. |
| `wrapfail.py --repo . --first N --trx ... --class X=path` | Wrap every plain override that fails in the given runs with `SpecFails.Throws/ThrowsAsync` plus a `// Fails:` tag, grouping tickets by message. |
| `classify-failures.py <trx>` | Summarize a TRX: tests that now pass (wrapper no longer needed), different exception, MQL differs, other. |
| `rebaseline.py report\|apply` | Re-baseline `SpecFails` overrides per EF version into `#if` arms. Env: `REPO_ROOT`, `RES_DIR` (holds `ef<N>-default.trx` and `harvest-ef<N>-default.tsv`), `PORT_FILES` (text file of repo-relative `.cs` paths), `VERS` (default `8,9,10,11`). Check the output for generated names like ``Foo`1[[...]]`` and rewrite to `Foo<global::...>`. |
| `SpecFails.harvest.cs` | Temporary drop-in for `Utilities/SpecFails.cs`: every helper records what the base test really did to the TSV named by `SPEC_FAILS_HARVEST` instead of asserting. **Keep its public API in sync with `SpecFails.cs`** (it replaces the file during a harvest run). |
| `linux/run_linux.sh`, `run_linux11.sh`, `rebaseline_linux.sh` | Reproduce CI under WSL/Linux with `TZ=UTC`. |

## Workflow

1. Normal run per EF version (TRX; EF11 is an xUnit v3 exe: `-noColor -class <ns.Class> -result-trx`).
2. Harvest run: copy `SpecFails.harvest.cs` over `SpecFails.cs`, run with `SPEC_FAILS_HARVEST=<tsv>`, `git checkout` the file.
3. `rebaseline.py report`, then `apply`; rebuild all four versions and re-run.
4. Always verify on Linux in UTC: a developer machine in another time zone gives different results (EF-X720).

## Linux/WSL setup

Needs a clone to build in (`CLONE`, default `~/work/ef`), .NET 10 SDK in `~/dotnet10`, .NET 11 SDK in `~/dotnet11`, and a MongoDB
container on port 27099 (`mongodb/mongodb-atlas-local:latest`; the scripts set `MONGODB_URI=mongodb://localhost:27099/?directConnection=true`
and `ATLAS_URI=Disabled`). `SRC` (default: this checkout) is fetched into `CLONE` at `BRANCH` (default `release`) and hard-reset.
`OUT_ROOT` (default `~/work/spec-out`) receives TRX files. Set `MONGODB_EF_QUERY_MODE=NativeOnly` via the third argument of `run_linux.sh`
to reproduce the CI `native-only` job. Python is run with `-I`.
