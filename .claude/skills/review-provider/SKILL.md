---
name: review-provider
description: Domain-aware fan-out review of uncommitted work, the current branch, an external PR, or a branch in another clone — routes changed files to the per-area reviewers in .claude/agents, adds three cross-cutting lenses, and aggregates one report. With no args it reviews everything since the branch point from release, including uncommitted and untracked files.
argument-hint: "[--all] [--uncommitted] [--model opus|sonnet|haiku] [<PR#> | <clone-path> [<base-ref> [<head-ref>]] | <base-ref> [<head-ref>]]"
allowed-tools: Bash, Read, Glob, Grep, Write, Agent
---

# /review-provider

Run the reviewer sub-agents (`.claude/agents/*-reviewer.md`) over a change set in parallel and produce one consolidated report. Modes:

- **Working tree** (default, no refs): the current branch's commits since it left the base branch **plus staged, unstaged and untracked files**. This is the "review what I'm about to commit" mode.
- **Uncommitted only** (`--uncommitted`): just what is not committed yet (staged, unstaged, untracked), against `HEAD`.
- **Committed range** (explicit `<base> <head>`): only committed history, exactly `<base>...<head>`.
- **External PR**: fetch a GitHub PR and diff it against its base.
- **External clone**: diff a branch in a *different* clone, running with the current clone's reviewer briefs and `AGENTS.md` files (for branches that lack the latest agent updates).

For a quick generic bug hunt with no domain reviewers, use the built-in `/code-review`; this skill adds the provider-specific briefs, the cross-cutting lenses and the repro-before-reporting rule.

User args: `$ARGUMENTS`

## Step 1: Determine scope

Parse `$ARGUMENTS`:
- `--all`: queue every reviewer regardless of diff; skip step 2 filtering.
- `--uncommitted`: working-tree mode with base = `HEAD` (ignore any base ref).
- `--model <name>` (`opus`, `sonnet` or `haiku`): pass as `model` on every dispatch in step 3. If absent, omit it so each reviewer uses its frontmatter default.
- Remaining non-flag tokens: try external PR mode, then external clone mode, then local mode.

**External PR mode**: the first token is a PR number (`297` or `#297`). Use the REST API: `gh pr view` goes through GraphQL, which fails on this machine's SAML-restricted token.
1. `gh api repos/{owner}/{repo}/pulls/<PR#> --jq '{number,title,url:.html_url,base:.base.ref,head:.head.sha,repo:.base.repo.full_name}'` (`{owner}/{repo}` resolves from the current clone's remote). For a PR in another repo, use `gh api repos/<owner>/<repo>/pulls/<PR#>` with the repo the user named.
2. `git remote -v`; pick the first remote whose fetch URL contains the PR's `<owner>/<repo>` (case-insensitive, HTTPS or SSH); else try `upstream`, then `origin`.
3. `git fetch <remote> refs/pull/<PR#>/head`; head SHA = `git rev-parse FETCH_HEAD`.
4. `git fetch <remote> <base>`.
5. **base ref** = `<remote>/<base>`; **head ref** = the SHA; **diff-repo** = `git rev-parse --show-toplevel`. Record PR number, title, URL and remote for the report header. Committed-range semantics apply (no working tree).

**External clone mode**: the first token is an existing directory.
1. `<clone>` = absolute path of it; use absolute forms everywhere.
2. Verify with `git -C "<clone>" rev-parse --show-toplevel`; if it fails, tell the user it is not a git checkout and stop.
3. Head label = `git -C "<clone>" rev-parse --abbrev-ref HEAD`, or `--short HEAD` if that returns `HEAD` (detached).
4. Next tokens: **base ref**, **head ref** (same defaults and rules as local mode, evaluated inside the clone).
5. **diff-repo** = `<clone>`. Run every git command as `git -C "<diff-repo>" ...` and pass reviewers absolute paths (`<diff-repo>/<repo-relative-path>`). The parent's working directory stays in the current clone so its `AGENTS.md` files and briefs load; that is the point of the mode.

**Local mode**: tokens are refs in the current repo; **diff-repo** = `git rev-parse --show-toplevel`.
- **base ref** default: `release` if `git rev-parse --verify -q release` succeeds, else `origin/release`, else `main`. (In this fork `main` mirrors upstream and work targets `release`; diffing against `main` drags in unrelated upstream commits.) An explicit base always wins.
- **head ref** omitted ⇒ **working-tree mode**. Head ref given ⇒ committed range.

### Change set

| Mode | Diff command `<diff>` | Files in scope |
|---|---|---|
| Working tree | `git -C "<diff-repo>" diff <mb>` where `<mb>` = `git -C "<diff-repo>" merge-base <base> HEAD` (`HEAD` for `--uncommitted`) | `<diff> --name-status`, plus untracked files from `git -C "<diff-repo>" ls-files --others --exclude-standard` (status `U`) |
| Committed range / PR | `git -C "<diff-repo>" diff <base>...<head>` | `<diff> --name-status` |

`git diff <mb>` compares that commit with the working tree, so it covers committed, staged and unstaged changes to tracked files in one diff. Untracked files have no diff; reviewers read them whole. Deleted files (`D`) have no current content; reviewers use the diff.

Unless `--all`, if the file list is empty, say what was compared (base, `<mb>`, whether the tree is clean) and dispatch nothing. If the tree is clean and the branch is at its base, suggest an explicit base (`/review-provider HEAD~3`) or `--all`.

Before dispatching, print one line stating the scope, e.g. `Reviewing EF-123 against release (merge-base 1a2b3c4): 6 committed files, 2 staged, 1 unstaged, 1 untracked.`

Examples: no args reviews branch + working tree against `release`; `--uncommitted` only the uncommitted work; `297` or `#297` an external PR; `HEAD~3 HEAD` the last three commits only; `release HEAD` this branch's commits without the working tree; `~/code/efcore-pr release/10.0` that clone's work against its `release/10.0`.

## Step 2: Map changed files to reviewers

A file may match several reviewers; queue all. Files matching none go in "Unmapped changes" in the report.

| Reviewer | Path patterns |
|---|---|
| `query-reviewer` | `src/MongoDB.EntityFrameworkCore/Query/**` |
| `storage-reviewer` | `src/MongoDB.EntityFrameworkCore/Storage/**` |
| `metadata-reviewer` | `src/MongoDB.EntityFrameworkCore/Metadata/**` |
| `serialization-reviewer` | `src/MongoDB.EntityFrameworkCore/Serializers/**`; `src/MongoDB.EntityFrameworkCore/ChangeTracking/**` |
| `public-api-reviewer` | `src/MongoDB.EntityFrameworkCore/Extensions/**`; `src/MongoDB.EntityFrameworkCore/Infrastructure/**`; `src/MongoDB.EntityFrameworkCore/Design/**` |
| `diagnostics-reviewer` | `src/MongoDB.EntityFrameworkCore/Diagnostics/**` |
| `value-generation-reviewer` | `src/MongoDB.EntityFrameworkCore/ValueGeneration/**`; `src/MongoDB.EntityFrameworkCore/Metadata/Conventions/MongoValueGenerationConvention.cs` |
| `spec-conformance-reviewer` | `tests/MongoDB.EntityFrameworkCore.SpecificationTests/**`; `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Utilities/**`; `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Usings.cs` |
| `vector-search-reviewer` | files matching `VectorIndex*`, `VectorSearch*`, `BinaryVector*`, or `VectorSimilarity*` across `src/` and `tests/`; vector-search helpers in `MongoDatabaseFacadeExtensions`, `MongoQueryableExtensions`, `MongoIndexBuilderExtensions`, `MongoPropertyBuilderExtensions` |
| `encryption-reviewer` | `src/MongoDB.EntityFrameworkCore/CryptProvider.cs`; `src/MongoDB.EntityFrameworkCore/QueryableEncryptionType.cs`; files matching `QueryableEncryption*` across `src/` and `tests/`; encryption properties on `MongoOptionsExtension` (`KeyVaultNamespace`, `KmsProviders`, `CryptExtraOptions`, `CryptProvider*`); `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Encryption/**` |

**Test folders** mirror `src/` and route to the matching `src/` area reviewer (`Query/`, `Storage/`, `Update/`, `Metadata/`, `Mapping/`, `Serialization/`, `ValueGeneration/`, `Design/`). `Encryption/` routes to `encryption-reviewer`; `FunctionalTests/Utilities/` to `spec-conformance-reviewer`. `SpecificationTests/<Area>/` routes to both the area reviewer (assertion logic) and `spec-conformance-reviewer` (fixture/inheritance discipline).

**Reviewer definitions**: a change to `.claude/agents/<name>-reviewer.md` is reviewed by that same reviewer (cross-cutters too). Exception: `pr-summary-reviewer` only runs in external PR mode, so changes to its file are reviewed only via `/review-provider <PR#>`.

New reviewers under `.claude/agents/` need a row here.

## Cross-cutting reviewers (always run, unless the diff is doc-only)

Run on every invocation, additional to the table above; each applies one lens across the whole diff.

| Reviewer | Concern |
|---|---|
| `api-stability-reviewer` | Public-API / breaking changes |
| `ef-conformance-reviewer` | EF Core integration, multi-version compat, service registration, build-vs-runtime model |
| `security-reviewer` | Credential exposure, sensitive-data logging gate, KMS leaks, TLS, connection-string redaction |

**Skip condition.** Without `--all`, if every changed file matches a doc-only pattern (`*.md`, `*.txt`, `docs/**`, `LICENSE*`, `.github/**` excluding `.github/workflows/**`), and **nothing under `.claude/**` is ever doc-only**: agent and skill files set `tools:` and `allowed-tools:` and hold the instructions the agents execute, so a change there still gets all three lenses, skip all three and write `Skipped — diff is documentation-only.` under `## Cross-cutting findings`. Any non-doc file cancels the skip.

## PR-summary reviewer (external PR mode only)

In external PR mode also dispatch `pr-summary-reviewer`, in parallel with the rest; its output goes at the top of the report (before `## Summary`). Not run in the other modes (no PR body), and not subject to the doc-only skip.

## Step 3: Resolve agent types, then dispatch in parallel

**Pre-flight.** Before dispatching, compare every queued reviewer name with the agent types listed in the `Agent` tool's description.
- Listed ⇒ dispatch with `subagent_type: <reviewer-name>`.
- Not listed ⇒ **fallback**, **local and working-tree modes only** (the diff is the user's own work). `general-purpose` has Edit and Write, so the read-only brief is only a prompt sentence; never use it on untrusted input. In external PR and external clone modes **do not fall back**: skip that reviewer, list it under `## Summary` as `not run (agent type unavailable)`, and tell the user to fix the agent file's frontmatter. For a fallback, dispatch `general-purpose` and begin its prompt with the body of the current clone's `.claude/agents/<name>.md` (everything after the closing `---` of the frontmatter) under the heading `Your reviewer brief:`, then the templates below. Pass that file's frontmatter `model` as `model` unless it is `inherit`, or `--model` overrides. Add: `You are a read-only reviewer: do not edit, write or delete any pre-existing file, and create only your own Review_* repro files.`
- If any fallback was used, tell the user in one line which reviewers, and that an agent file whose frontmatter fails to parse is skipped silently by Claude Code (the usual cause is an unquoted `: ` in `description`; see `docs/agents-architecture.md`). Never let a missing agent type abort the run or fail the batch.

Emit a **single** assistant message with one `Agent` block per reviewer (matched area + feature reviewers, cross-cutters unless skipped, and `pr-summary-reviewer` in PR mode); only same-message calls run concurrently. Set `model` only as described above.

Substitute `<scope>` (a one-line description such as `branch EF-123 plus uncommitted work, against release (merge-base 1a2b3c4)`), `<diff>` (the command from the table above), `<diff-repo>` (the other clone in clone mode), and file lists as **absolute paths** `<diff-repo>/<repo-relative-path>` with their status (`M`, `A`, `D`, `R`, `U` = untracked) in every template.

### Area / feature reviewer prompt

```
You are running as part of a multi-area review. Scope: <scope>. Repo: <diff-repo>.

Files in scope for this iteration that fall in your area (absolute paths, with status):
- M <abs-file1>
- U <abs-file2>
…

Read those files at their current state. To see what changed in a file run `<diff> -- <repo-relative-path>` (git commands use `git -C "<diff-repo>" …`; the parent agent's working directory may not be <diff-repo>). `U` files are untracked: there is no diff, read them in full. `D` files no longer exist: use the diff. Pull in adjacent context only as needed to judge the change.

The working tree may hold uncommitted work that is not yours, and other reviewers run concurrently in it. Never modify, stash, reset, checkout or delete a pre-existing file. Name any repro file you create `Review_<your-reviewer-name>_*` so you can find and delete it afterward. If a build fails on a file lock or on a file you did not write, retry once, then report it as [external-action] rather than touching it.

Follow the report shape, tags, finding cap, and verification requirement in `.claude/agents/CONVENTIONS.md`. In short: verify every functional finding by running code before you report it — scaffold repros with `Bash` (you have no `Edit`/`Write`), clean up any repro file afterward, and include the repro and observed output in your report.
```

### Cross-cutter prompt

Pass the *full* changed-file list.

```
You are running as a cross-cutting reviewer in a multi-area review. Scope: <scope>. Repo: <diff-repo>.

Your concern is not scoped to a directory — it is a single hygiene lens applied across the change set. Files in scope (absolute paths, with status):
- M <abs-file1>
- U <abs-file2>
…

Use `<diff>` to see the full picture and `<diff> -- <repo-relative-path>` to focus. `U` files are untracked: read them in full. Read files at their current state where context matters. Skip files clearly irrelevant to your concern.

The working tree may hold uncommitted work that is not yours, and other reviewers run concurrently in it. Never modify, stash, reset, checkout or delete a pre-existing file; name any repro file `Review_<your-reviewer-name>_*` and delete it afterward.

Follow the report shape, tags, finding cap, and verification requirement in `.claude/agents/CONVENTIONS.md`. Findings must be specific to your concern — do not duplicate what an area reviewer would catch.
```

### PR-summary prompt (external PR mode only)

Pass the PR fields and the *full* changed-file list.

```
You are running as the PR summary reviewer in a multi-area review of an external pull request.

PR: #<number> — <title>
URL: <url>
Diff: `git -C "<diff-repo>" diff <base>...<head>` (repo at <diff-repo>)

All files changed in this range (absolute paths):
- <abs-file1>
- <abs-file2>
…

Pull the PR body yourself with `gh api repos/{owner}/{repo}/pulls/<number> --jq '{body,additions,deletions,changed_files,author:.user.login,labels:[.labels[].name]}'` (REST; `gh pr view` fails on this machine's token) to get the author's stated rationale. Use the diff command above and read files at their current state where context matters.

Produce a report in exactly the shape specified in your agent definition (Description / Assessment / Verdict; 500-word cap). Do not duplicate the per-area or cross-cutting reviewers' work.
```

## Step 4: Aggregate

After all reviewers return, show only one consolidated report (no raw transcripts).

Heading:
- **External PR**: `# PR review: #<number> — <title>`, then the PR URL and `diff range: <base>...<head>` on separate lines.
- **External clone**: `# Clone review: <head-label> in <clone>`, then the scope line.
- **Working tree**: `# Working-tree review: <branch> against <base>`, then the scope line (counts of committed / staged / unstaged / untracked files).
- **Committed range**: `# Branch review: <base>...<head>`

```
# <heading from above>

## PR summary
(External PR mode only — paste the `pr-summary-reviewer`'s Description, Assessment, and Verdict verbatim. Omit in other modes.)

## Summary
N reviewers ran (M area+feature + up to 3 cross-cutting [+ 1 PR summary in external PR mode]). X approved, Y flagged, Z escalated. (PR-summary verdict is not counted.) [If fallback dispatch was used: name the reviewers.]

## Escalations
For each `escalate` verdict from any reviewer — reviewer name, then its findings verbatim. Omit if none.

## Cross-cutting findings
Group by reviewer (`api-stability-reviewer`, `ef-conformance-reviewer`, `security-reviewer`): verdict + findings as bullets, or `<reviewer> — clean` if it approved. If the doc-only skip applied, write `Skipped — diff is documentation-only.` instead.

## Area + feature findings
For each area or feature reviewer that ran: verdict + findings as bullets if `flag`, or `<reviewer> — clean` if `approve`. (Escalations are already covered above.)

## Unmapped changes
Files that matched no area or feature reviewer (a coverage signal only; cross-cutters cover the whole diff). Omit if none.
```

After the report, confirm the working tree is as it was: run `git -C "<diff-repo>" status --short` and compare with the status captured before dispatch; if a `Review_*` repro file or any other unexpected change remains, list it (do not delete anything that pre-dates the run).

**Save to file.** Save in the parent agent's current working directory (the current clone), never `<diff-repo>` when those differ.

- **External PR**: stem `review<number>` (e.g. `review297`). Always saved.
- **External clone**: stem `review-<sanitized-head-label>`, replacing every character outside `[A-Za-z0-9._-]` with `-` (`EF-308a` gives `review-EF-308a`; `feature/foo` gives `review-feature-foo`). Always saved.
- **Working tree / committed range**: do not save; inline only.

When saving: list files matching `<stem>[a-z].md`, use the lowest unused letter (`a` if none), write the report to `<stem><letter>.md` with the Write tool, and end your response with one line, e.g. `Saved to review297a.md`.

## Notes

- Reviewers cannot fix the tree (`tools: Read, Grep, Glob, Bash`); treat suggested fixes as work for the user or a follow-up. They *must* use `Bash` to verify functional findings (`dotnet test`, throwaway repro via here-doc, `git diff`) and delete any repro file before returning. See `.claude/agents/CONVENTIONS.md`.
- Capture `git status --short` once before dispatch (working-tree mode) so the post-run check in step 4 has something to compare against.
- Pass an explicit `<base-ref>` for other bases (`/review-provider release/10.0`) and a second positional arg to cap the range end (`HEAD~5 HEAD~1`); giving a head ref excludes the working tree.
- In PR mode the remote is inferred from the PR URL, so forks and any named remote work without flags.
- Clone mode runs no `pr-summary-reviewer`; run `/review-provider <PR#>` separately for a PR summary.
- Model defaults are per reviewer, not inherited from the session: `value-generation-reviewer` and `diagnostics-reviewer` use `haiku`; the other area, feature and cross-cutting reviewers `sonnet`; `pr-summary-reviewer` `inherit`. `--model` overrides all uniformly.
- One EF configuration suffices to confirm a behavioral bug; `/test-all` is only needed for *divergence across* EF8/EF9/EF10/EF11, which (like Atlas-unavailable and encryption-infra paths) stays `[external-action]`.
