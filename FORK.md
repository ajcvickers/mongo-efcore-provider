# Fork notes

This fork publishes NuGet packages as **Unicorn.EntityFrameworkCore.MongoDB**. It is an unofficial build of
[mongodb/mongo-efcore-provider](https://github.com/mongodb/mongo-efcore-provider) that includes changes not yet merged upstream.
Assembly names and namespaces are unchanged, so it is a drop-in replacement for `MongoDB.EntityFrameworkCore`
(reference one or the other, not both).

## Branches

| Branch | Purpose |
|---|---|
| `main` | Exact mirror of `upstream/main`. Fast-forward only; never commit here. |
| `EF-nnnn` | One change each, cut from `upstream/main`, used for the upstream PR. |
| `release` | `upstream/main` + merged feature branches + fork-only files (this file, the publish workflow). Packages are built from here. |

## Workflow

1. New work: `git switch -c EF-nnnn upstream/main`, then open the PR upstream from this branch.
2. Also merge it into `release`.
3. Sync with upstream: fast-forward `main` (`git fetch upstream && git switch main && git merge --ff-only upstream/main && git push origin main`), then `git switch release && git merge main`.
4. Publish: tag `release` as `v<upstream version>.<fork revision>`, e.g. `v10.0.5.1`, and push the tag. The first number selects the EF Core line (8, 9 or 10).
   Needs the `NUGET_API_KEY` repository secret.

## Changes not yet in upstream

<!-- Keep this list current: remove entries as the PRs merge upstream. -->
- (none yet)
