# Fork notes

This fork publishes NuGet packages as **Unicorn.EntityFrameworkCore.MongoDB**. It is an unofficial build of
[mongodb/mongo-efcore-provider](https://github.com/mongodb/mongo-efcore-provider) that includes changes not (yet) merged upstream,
and may stay well ahead of upstream. Assembly names and namespaces are unchanged, so it is a drop-in replacement for
`MongoDB.EntityFrameworkCore` (reference one or the other, not both).

Day-to-day steps are in [FORK-WORKFLOW.md](FORK-WORKFLOW.md).

## Branches

| Branch | Purpose |
|---|---|
| `release` | The trunk. `upstream/main` + all fork changes + fork-only files (these docs, the publish workflow). Fork-only work is branched from and merged into it. Packages are built from it. |
| `main` | Exact mirror of `upstream/main`. Fast-forward only; never commit here. |
| `EF-nnnn` (upstream-bound) | A change that does not depend on fork-only work. Cut from `upstream/main`; becomes an upstream PR. |
| `EF-nnnn` (fork-only) | A change that depends on fork-only work, such as EF-322d. Cut from `release`; PR'd and merged inside the fork. |

Every change lives on its own branch and reaches `release` by merge. Nothing is committed straight to `release`,
except the fork-only files and the occasional merge-conflict resolution. This is what makes it possible to extract a change for
upstream later.

## Versions

Tags are `v<upstream version>.<fork revision>`, e.g. `v10.0.5.1`, and select the EF Core line by their first number (8, 9 or 10).
The upstream part is the nearest upstream release, not a promise that the code matches that release exactly. The fork is
usually well ahead of it. Use revision `.1` or higher: NuGet drops a trailing `.0`.

## Publishing

Tag `release` and push the tag. `publish-nuget.yml` builds, tests, packs and pushes with NuGet trusted publishing (OIDC). It needs a
trusted publishing policy on nuget.org for this repo and workflow file, and the `NUGET_USER` repository variable (the nuget.org
username). No API key is stored.

## Changes not in upstream

<!-- Keep current. Remove an entry when its change is merged upstream. -->

| Change | State |
|---|---|
| EF-322d | Foundation for most other fork work. Merged into `release` (PR #8). Not offered upstream. |
