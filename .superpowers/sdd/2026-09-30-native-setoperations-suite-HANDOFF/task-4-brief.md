### Task 4: Bring in Negate (Select Task 1), regenerate baselines, verify everything

- [ ] **Step 1: Pull in the Negate commit.** Ask the owner whether Select Task 1 has landed on `EF-322c`, then run
  `git fetch /Users/arthur.vickers/code/mongo-efcore-provider EF-322c && git rebase EF-322c`.
  - Resolve conflicts in `NorthwindSetOperationsQueryMongoTest.cs` by regenerating baselines, never by hand-merging.
  - If Select Task 1 has not landed, **stop and report**. Do not implement Negate here.
- [ ] **Step 2: Remeasure and regenerate.** Run the suite under NativeOnly on EF10 and expect **0 failures**. Regenerate
  every baseline that differs in default mode. The triage predicted 9 churners after N, A and C: column_binary,
  column_constant, column_function, column_unary, columns_with_different_nullability, constant_function,
  function_column, function_unary and unary_function.
- [ ] **Step 3: Full-suite verification on each version.** For each of EF8, EF9 and EF10:
  - the SetOps suite under NativeOnly;
  - the **full** spec suite in default mode;
  - the full FunctionalTests project in default mode.
  Per-class verification has missed regressions before. Report a re-derived before/after count table for each version.
- [ ] **Step 4: Update docs.** Add the supported set-operation operand shapes to `docs/native-query-status-EF-322.md`.
  If the gate's shape changed materially, add a line to `src/MongoDB.EntityFrameworkCore/Query/AGENTS.md`.
- [ ] **Step 5: Review and hand back.** Run `/review-ef-core-provider` and fix Critical/Important findings. Commit, then
  report to the owner the branch name, the commit list, the count tables, any `// Fails:` added (there should be none)
  and any follow-ups. **Do not push or merge.**

## Out of scope

- `*_scalarsubquery` on the right side (source2 with lookups). Driver-LINQ fails too, and the existing failure overrides are correct.
- Constants inside anonymous or DTO projections on source1.
- A join-scope **source2**.
