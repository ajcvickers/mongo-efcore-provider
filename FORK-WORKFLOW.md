# Fork workflow

How to work with this fork (`ajcvickers/mongo-efcore-provider`). The fork publishes `Unicorn.EntityFrameworkCore.MongoDB` and is
expected to stay well ahead of upstream (`mongodb/mongo-efcore-provider`), because much of the work stacks on EF-322d, which may never
be merged there. See `FORK.md` for the branch model.

Remotes: `origin` = the fork, `upstream` = mongodb/mongo-efcore-provider.

The model in one paragraph: **`release` is the trunk.** Fork-only work branches from `release`, is PR'd inside the fork, and merges back.
Work that does not depend on fork-only code branches from `upstream/main` instead, goes upstream as a PR, and is then merged into `release`.
`main` only mirrors upstream.

---

## 0. One-time setup

```
git config rerere.enabled true      # remember conflict resolutions and replay them on later merges
```

Already done on GitHub (listed here for reference if the fork is ever recreated):
- `release` is the fork's default branch, so it is where PRs and visitors land.
- `.github/workflows/github-actions-atlastest.yml` runs on PRs to `release` (it originally listed only `main` and `release/*`).

---

## 1. Which kind of change is this?

| The change... | Branch from | Goes to |
|---|---|---|
| does not depend on fork-only code (bug fix, small feature that also makes sense on upstream) | `upstream/main` | upstream PR, **and** merge into `release` |
| depends on fork-only code (EF-322d or anything built on it) | `release` | in-fork PR into `release` only |

If in doubt, try the upstream-bound route first: write the change on `upstream/main`. If it won't compile or work without fork-only
code, it is fork-only.

---

## 2. Fork-only change

### 2.1 Build it

```
git fetch origin
git switch -c EF-nnnn release          # use fork-<name> if there is no JIRA number
```

- Commit messages start with the JIRA number: `EF-nnnn: Description`.
- Keep the branch **linear and focused on one feature**, with meaningful commits. You may want to extract those commits later
  (section 5).
- If `release` moves while you work, `git rebase release` is fine on this branch, because it is private to you and the fork.
- Build and test: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`, or run `test-all.cmd` / `./test-all.sh` (or `/test-all`) for EF8, EF9, EF10 and EF11 in both query modes.

### 2.2 In-fork PR

```
git push -u origin EF-nnnn
gh pr create --repo ajcvickers/mongo-efcore-provider --base release --head EF-nnnn
```

Use the PR for the record and for CI. Review is optional if it is only you.

### 2.3 Merge into `release`

```
git switch release
git pull --ff-only origin release
git merge --no-ff EF-nnnn -m "Merge EF-nnnn: Description"
```

`--no-ff` keeps a merge commit. That marks where the feature starts and ends, which makes it easy to revert or to extract later:
`git log --oneline <merge>^1..<merge>^2` lists exactly its commits.

Then add or update the row in "Changes not in upstream" in `FORK.md`, build and test on `release` (`/test-all`) and
`git push origin release`.

To ship, see section 6.

---

## 3. Upstream-bound change (does not depend on fork-only code)

### 3.1 Build it on a clean base

```
git fetch upstream
git switch -c EF-nnnn upstream/main
```

- Commit messages and the PR title start with the JIRA number: `EF-nnnn: Description`.
- Check `BREAKING-CHANGES.md` before opening the PR. Breaks are measured against the latest *released* package.
- Build and test on this branch alone (`/test-all`): the point is that it works against upstream without fork code.

### 3.2 Open the upstream PR

```
git push -u origin EF-nnnn
gh pr create --repo mongodb/mongo-efcore-provider --base main --head ajcvickers:EF-nnnn --title "EF-nnnn: Description"
```

### 3.3 Bring it into `release`

```
git switch release
git merge --no-ff EF-nnnn -m "Merge EF-nnnn: Description"
```

This is where conflicts with fork-only rewrites (EF-322d) show up. Resolve them here, in `release`, then build and test. rerere
remembers the resolution if the branch is merged again. List the change in `FORK.md` and push.

The **same fix often has to be done twice**: once the way upstream's code looks, once adjusted to the fork's. That is the price of
a PR that upstream can take. Put the adjustment in the merge resolution, or in a small fork-only commit on `release` straight after the merge.

### 3.4 Review feedback on the open PR

Commit to `EF-nnnn`, push it, then `git switch release && git merge EF-nnnn && git push origin release`.

If upstream moved and the PR now conflicts, **merge** upstream into the branch and push:

```
git switch EF-nnnn
git merge upstream/main
git push origin EF-nnnn
```

Don't rebase a branch that is already merged into `release`: it would get new commit hashes, and the next merge would
conflict with the old ones. Upstream squashes PRs, so the PR branch's history doesn't matter to them.

---

## 4. Syncing with upstream

Do this regularly, weekly or after each upstream merge. Small frequent merges are much easier than one large one, and the more the fork diverges, the more
this matters.

### 4.1 Update the mirror

```
git fetch upstream
git switch main
git merge --ff-only upstream/main
git push origin main
```

If `--ff-only` fails, something was committed to `main` by mistake. Move it to a branch, then reset `main` to `upstream/main`.

### 4.2 Merge into `release`

```
git switch release
git merge main
```

Resolve conflicts (rerere will reapply earlier resolutions), build, run `/test-all`, and push. Where an upstream change collides with
a fork-only rewrite (such as EF-322d), the fork's behaviour is generally the one to keep, with the upstream fix re-applied on top.

### 4.3 When upstream merges one of your PRs

The merge usually conflicts or duplicates, because upstream squashed your commits and often edited them in review.

- For the files of that change, use upstream's version where it is the final word. Here `main` is the side being merged in, so
  `git checkout --theirs <file>`, then `git add` it.
- Where the fork needs more than upstream took, keep the extra parts as a small fork-only change.
- Remove the entry from "Changes not in upstream" in `FORK.md`, and delete the branch:
  `git branch -d EF-nnnn; git push origin --delete EF-nnnn`.

---

## 5. Extracting fork-only work for upstream (optional, and only if it can stand alone)

Only possible when upstream has what the change depends on: EF-322d has landed, or the change turns out to need nothing from it.

```
git fetch upstream
git switch -c EF-nnnn-upstream upstream/main
git cherry-pick <merge>^1..<merge>^2        # the feature's commits, found from its merge commit on release
```

Resolve, build, test, then open the PR as in 3.2. Once upstream has merged it, section 4.3 applies.

If EF-322d is split up, preparatory refactorings that don't change behaviour can go upstream first. That shrinks the gap even if the
core never lands.

If EF-322d itself lands upstream: sync (4.1–4.3), then take the pending fork-only changes through section 5 one at a time.

---

## 6. Shipping a package

1. Be on a tested `release` that is pushed: `/test-all`, then `git push origin release`.
2. Pick the base versions, i.e. upstream's latest release per EF line:
   ```
   gh release list --repo mongodb/mongo-efcore-provider --limit 10
   ```
   With upstream at `v10.0.5`, `v9.1.5` and `v8.4.5`, the first fork builds are `v10.0.5.1`, `v9.1.5.1` and `v8.4.5.1`. The next build
   on the same base is `.2`. When upstream releases a new base such as `v10.0.6`, restart at `v10.0.6.1`.
3. Tag and push one tag per EF line to ship (the first number selects `Release EF8|9|10|11`):
   ```
   git tag v10.0.5.1 release
   git push origin v10.0.5.1
   ```
4. Watch "Publish NuGet" in the fork's Actions tab. It builds, runs the unit and functional tests, packs, pushes to NuGet.org via trusted
   publishing, and creates a GitHub release with the packages.

Notes:
- Published versions can be unlisted but never deleted. Don't use real version tags for experiments.
- Don't move a tag that has been published. Fix `release` and bump the revision.
- A `.0` revision publishes as the three-part version (`v10.0.5.0` → `10.0.5`). Start at `.1`.
- EF11 has no upstream release to base a version on. Use `11.0.0` as the base (e.g. `v11.0.0.1`). EF Core 11 is GA, so publish it as a
  stable version. If `Versions.props` ever points at a prerelease EF Core again, publish a prerelease too (`v11.0.0.1-rc.1`): a stable
  package that depends on a prerelease EF Core triggers NuGet's NU5104 warning and misleads consumers. The tag pattern and the
  first-number rule work with the suffix.
- The publish workflow lives on `release` only. Never put it on an upstream-bound branch.

---

## 7. Several checkouts at once

This fork is usually worked on from **more than one checkout at the same time**, each with its own Claude session (and sometimes an
IDE open on it): `D:\code\mongo-efcore-provider`, `D:\code\provider2`, and later `provider3`, and so on. They are independent full
clones of `origin`, not `git worktree`s, so they share nothing except GitHub, the machine, Docker and the NuGet cache. Each
checkout normally has a different branch and task. (A throwaway worktree such as `%TEMP%\ef479-benchefore` is a baseline
for benchmarking, not a place to work.)

Rules for an agent in any checkout:

1. **Stay in your own checkout.** Don't read, edit, build, test or run git in a sibling checkout unless the user asks. Another
   session may be in the middle of an edit or a test run there. Find your root with `git rev-parse --show-toplevel`.
2. **One branch per checkout.** Don't check out a branch that is checked out in another clone, and don't assume a branch you
   see on `origin` is yours: it may be another session's work. `git fetch origin` before you push; never force-push, and never
   rebase, reset or delete a branch you did not create in this session without asking.
3. **GitHub is the shared state.** Issue and PR numbers are global to the fork. `gh issue list` / `gh pr list` before filing, so
   two sessions don't open duplicates, and reference the existing one.
4. **Docker and ports.** Every test process starts its own container on a random port, so concurrent runs don't collide. Don't
   stop, remove or prune containers you did not start in this session: they may belong to a run in another checkout. If
   `MONGODB_URI` or `ATLAS_URI` points at a real server, all checkouts share it: run serially (`test-all --shared-db` does this; by default `test-all` ignores them and uses a container per test process).
5. **Expect load.** Two full test runs at once are slow and can cause timeouts that are not real failures; if a failure looks
   like a timeout or a Docker error, re-run it alone before concluding anything.
6. **Checkouts differ.** An older branch may lack files and conventions that exist elsewhere (this document included). Read the
   files in *your* checkout, not what you remember from another.
7. **Memory and scratch are per checkout.** Claude's project memory is keyed by directory, so it does not carry across clones.
   Anything another checkout needs to know goes in a tracked doc (or a GitHub issue), not only in memory. Use the session
   scratchpad for temporary files, never a shared path such as `/tmp`.

New checkout: `git clone https://github.com/ajcvickers/mongo-efcore-provider.git D:\code\providerN`, then
`git remote add upstream https://github.com/mongodb/mongo-efcore-provider.git` and `git config rerere.enabled true`. The user-level
`~/.claude/CLAUDE.md` already applies these rules to `D:\code\provider*`, including checkouts that predate this section.

---

## Quick reference

| I want to... | Do |
|---|---|
| Start a fork-only change | `git switch -c EF-nnnn release` |
| Start an upstream-bound change | `git fetch upstream; git switch -c EF-nnnn upstream/main` |
| Open an upstream PR | `gh pr create --repo mongodb/mongo-efcore-provider --base main --head ajcvickers:EF-nnnn` |
| Land a change in the fork | `git switch release; git merge --no-ff EF-nnnn`; update `FORK.md`; push |
| Sync with upstream | ff-only `main` from `upstream/main`; `git merge main` on `release` |
| Find a feature's commits | `git log --oneline <merge>^1..<merge>^2` |
| Ship | tag `release` as `v<base>.<rev>` and push the tag |
| Check upstream's latest versions | `gh release list --repo mongodb/mongo-efcore-provider` |
