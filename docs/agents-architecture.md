# Per-area `AGENTS.md` & reviewer sub-agents

The repo is partitioned into functional areas so subdirectory auto-loading scopes agent context. Each area has an `AGENTS.md` (with a sibling `CLAUDE.md` containing `@AGENTS.md`, or `@../OtherDir/AGENTS.md` when sibling directories share one) and a read-only reviewer in `.claude/agents/` (`tools: Read, Grep, Glob, Bash`). `/review-provider` (`.claude/skills/review-provider/SKILL.md`) fans out to the reviewers. Pattern adapted from `mongo-csharp-driver` PR #1993.

Each area `AGENTS.md` carries YAML frontmatter (`area`, `scope` globs, `reviewer-agent`, `adjacent-areas`); Claude Code doesn't parse it, but humans and tooling do. Report shape and verification rules for all reviewers: `.claude/agents/CONVENTIONS.md`. The area table is in root `AGENTS.md`.

## Boundary decisions

- **Serializers + ChangeTracking** are one area: a new collection type needs both a serializer and a value comparer. `ChangeTracking/CLAUDE.md` points at `Serializers/AGENTS.md`.
- **Extensions + Infrastructure + Design** are one area (`public-api-reviewer`); `Infrastructure/` and `Design/` `CLAUDE.md` point at `Extensions/AGENTS.md`.
- **Metadata vs. Extensions:** builder methods (`.ToCollection(...)`, `.HasElementName(...)`) live in `Extensions/` but their semantics live in `Metadata/`. A new annotation is a `metadata-reviewer` concern, the builder surfacing it a `public-api-reviewer` concern; most PRs touch both.
- **ValueGeneration has its own reviewer** rather than folding into Metadata: selector ordering is a distinct invariant, and confusing it with the Metadata convention that decides *whether* generation runs causes silent regressions.
- **Vector search and encryption are feature reviewers**, scoped by file glob (`VectorIndex*`, `BinaryVector*`, `VectorSearch*`; `CryptProvider.cs`, `QueryableEncryption*`) with no `AGENTS.md`. They span Query, Metadata, Storage and Extensions, so a directory home would be fake, and cross-cutter status would run them on every diff.
- **`spec-conformance-reviewer`** owns test infrastructure (`Utilities/`, `Usings.cs`) and spec-inheritance discipline only; per-area test folders mirror `src/` and go to the matching area reviewer.

## Cross-cutting reviewers

`api-stability-reviewer`, `ef-conformance-reviewer` and `security-reviewer` have no `AGENTS.md` or path scoping and run on every review (skipped for doc-only diffs). There is deliberately no `async-reviewer` (the driver repo has one): the provider has no sync/async pairing to enforce, so it would mostly approve. `ef-conformance-reviewer` takes its place because multi-EF-version compatibility is the cross-cutting concern unique to provider authors. If async issues recur, add the agent file and a row to the cross-cutters tables in root `AGENTS.md` and `SKILL.md`.

`pr-summary-reviewer` runs only in external PR mode (needs a PR body) and gives the "what is this PR and is it a good change" opinion at the top of the report.

## External clone mode

`/review-provider <path>` reviews a branch in another clone using the *current* clone's reviewer briefs and `AGENTS.md` files, for when agent definitions have moved on but the PR branch hasn't picked them up. The parent stays in the current clone; file lists are absolute paths under the other clone and git runs as `git -C "<clone>" ...`.

## Agent registration

Claude Code registers a `.claude/agents/*.md` file as a subagent type only if its YAML frontmatter parses; a file that fails to parse is **skipped silently**. The usual cause here was an unquoted `: ` inside `description` (`Boundary with metadata-reviewer: that owns …`), which YAML reads as a nested mapping. Twelve of the fourteen reviewers were unregistered for that reason and `/review-provider` failed on every dispatch. Rules for `description`:

- Keep it one line, with no `: ` and no ` #`; use ` — ` or `;` instead, or wrap the whole value in double quotes (then escape inner quotes).
- Agent files are read at session start, so restart the session (or `/agents`) after editing one.
- To check registration, run `claude -p --model haiku "List every subagent_type ending in -reviewer"` in the repo; all 14 reviewers should appear. `/review-provider` also detects missing types. For your own local or working-tree diff it falls back to `general-purpose` agents with the agent file inlined; for an external PR or clone it skips that reviewer instead, because `general-purpose` can write files and the diff is untrusted. Either way a broken file degrades the run rather than failing it.

## Review modes

`/review-provider` with no refs reviews the branch's commits since it left `release` **plus staged, unstaged and untracked files**; `--uncommitted` reviews only the uncommitted part. An explicit head ref restricts it to committed history. The default base is `release` (falling back to `origin/release`, then `main`) because `main` mirrors upstream in this fork. The built-in `/code-review` is the generic alternative; this skill earns its keep through the per-area briefs, cross-cutting lenses and repro-before-reporting rule.

## Adding an area

1. Add `AGENTS.md` (frontmatter as above) and a sibling `CLAUDE.md` containing `@AGENTS.md`.
2. Add `.claude/agents/<name>-reviewer.md` (frontmatter `name`, `description` with the globs and boundary, `tools: Read, Grep, Glob, Bash`, `model`). Point at the area `AGENTS.md` and `CONVENTIONS.md` rather than restating invariants; add only an "Also check" list and an "Escalate to user when" list.
3. Update the area table in root `AGENTS.md` and the Step 2 path table in the skill's `SKILL.md`.
4. Check: every `CLAUDE.md` is one `@` line; the matching `AGENTS.md` surfaces when opening a file in the area; a `dotnet test --filter` from its Testing section passes on a clean checkout.
