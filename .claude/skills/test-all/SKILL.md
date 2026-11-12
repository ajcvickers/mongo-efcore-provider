---
name: test-all
description: Build and run the tests for EF8, EF9, EF10 and EF11 in both Default and NativeOnly query modes (8 parallel runs, each test process in its own Docker container) via test-all.sh
argument-hint: "[--versions 10,11] [--mode both|default|native] [--projects unit,functional,spec] [--filter TEXT] [--parallel N] [--no-build]"
allowed-tools: Bash(bash ./test-all.sh), Bash(bash ./test-all.sh *), Read, Glob, Grep
---

# Test All EF Versions

All the work is done by `test-all.sh` / `test-all.cmd` in the repository root (a thin wrapper over `tests/tools/test-all.py`).
The same script runs from any command prompt without this skill, so **do not re-implement any of it here**: no sub-agents, no
hand-built `dotnet` command lines, no log scraping, no adding up of counts. The script reads TRX result files, which is why
its numbers are the ones to report.

By default it runs **8 runs in parallel** — EF8, EF9, EF10, EF11 × Default and NativeOnly (`MONGODB_EF_QUERY_MODE=NativeOnly`,
the native translator with no driver-LINQ fallback; CI runs it and it must have no failures) — each running UnitTests,
FunctionalTests and SpecificationTests. Every test process starts its own MongoDB Atlas Local container, and the script
clears `MONGODB_URI`/`ATLAS_URI` so no run touches a shared server. It also fetches `crypt_shared` + `mongocryptd` so the
encryption tests run. EF11's SpecificationTests are run as an executable (xUnit v3 4.x cannot run under `dotnet test`); the
script handles that.

## Run it

`$ARGUMENTS` are passed through unchanged (`--help` lists them). Common ones: `--versions 10,11`, `--mode native`,
`--projects unit,functional` (skips the long SpecificationTests), `--filter <text or VSTest expression>`, `--parallel N` on a
small machine, `--no-build`, `--shared-db` (use `MONGODB_URI`; runs serially).

1. Run it **in the background** (Bash `run_in_background: true`), because a full run takes far longer than the 10-minute
   foreground limit of the Bash tool:

   `bash ./test-all.sh $ARGUMENTS`

   Use the working directory's script (never a sibling checkout's). Do not poll; you are notified when it exits. The script
   prints a progress line per finished test project and a "still running" line every 5 minutes, and ends with a report.
2. When it exits, read the report (the end of the output, or the `summary.md` named on its last line) and relay it: the
   Default and NativeOnly tables, the "Problems" section, the NativeOnly-only failures section, and the
   `Encryption: ...` line, which says whether the encryption tests ran. Exit code 0 = all green, 1 = failures or incomplete
   runs, 2 = usage or pre-flight error (message on stderr; relay it, usually Docker is not running or an SDK/runtime is
   missing).
3. A run marked `INCOMPLETE`, `NO RESULTS` or `TIMEOUT` is not a pass and not a small number of failures: the test process
   crashed, hung or never started. Say so; the log path is in the report.
4. With eight runs sharing one machine, a failure can be load (timeouts, container start-up) rather than real. Before
   reporting a failure as real, re-run only that run with the command the report prints under "re-run alone" (for example
   `bash ./test-all.sh --versions 10 --mode native --projects functional --no-build`). Report both outcomes if they differ.
5. A failure under NativeOnly that does not fail in Default needs an `IsNativeOnly` branch in the test (or is a translator
   gap); a failure in both modes is a plain failure. The report already separates the two.

Never edit tests, baselines or product code from this skill; it only runs and reports. Do not stop or remove Docker
containers: the ones the tests start are cleaned up by TestContainers, and others may belong to other sessions.
