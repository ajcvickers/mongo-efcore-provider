# Task 22 Report — Cover the remaining setter shapes

## What was added

File: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` (test-only; no `src/` changes).

1. **Model additions** (private nested types, placed after `ConfigureModel`): `Priority` enum, `RichOrder` (with `Status`, `Priority`, `Correlation` (Guid), `OccurredAt` (DateTime), `Quantity`, and an owned `Shipping` nested `Address`), and `ConfigureRichModel`.

2. **Four tests**, in order:
   - `ExecuteUpdate_setting_null_writes_null` — exactly as the brief specified.
   - `ExecuteUpdate_setting_enum_Guid_and_DateTime_writes_the_same_BSON_as_SaveChanges` — **renamed** from the brief's `..._round_trips_like_SaveChanges` and rewritten per Correction 1: it seeds a second `RichOrder` document written entirely through `SaveChanges` with the same `Priority`/`Correlation`/`OccurredAt` values, bulk-updates the first document to the same values, then reads both back **raw** via `collection.Database.GetCollection<BsonDocument>(...)` (the pattern already used a few tests above, by `ExecuteUpdate_constant_set_on_value_converter_property_persists_converted_shape`) and asserts `BsonType` and value equality field-by-field for all three fields. It also keeps an EF-mediated read-back of the bulk-updated document as a self-consistency check, with `Kind` asserted explicitly since `Assert.Equal` on `DateTime` ignores it.
   - `ExecuteUpdate_targeting_an_owned_nested_property_throws` — exactly as the brief specified.
   - `ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null` — **kept the brief's name** (it turned out to be accurate) but per Correction 2 now reads the resulting document back raw and asserts the field is present with `BsonType.Null`, rather than only asserting the matched count.

## Raw-BSON comparison result for enum/Guid/DateTime (the claim `SerializeConstant`'s XML comment makes)

Captured via a temporary probe (added, observed, then removed before finalizing — not committed) inside the new test, on EF10:

```
bulkDoc:        { "Correlation": { "$binary": { "base64": "K4zR56UxS7KFHaqRPcbG6Q==", "subType": "04" } }, "OccurredAt": { "$date": "2026-07-30T12:00:00Z" }, "Priority": 1, ... }
saveChangesDoc: { "Correlation": { "$binary": { "base64": "K4zR56UxS7KFHaqRPcbG6Q==", "subType": "04" } }, "OccurredAt": { "$date": "2026-07-30T12:00:00Z" }, "Priority": 1, ... }
```

Identical in every respect: `Priority` (enum `High`) as BSON int32 `1` in both; `Correlation` (Guid) as BSON binary subtype `04` (standard UUID) with identical bytes in both; `OccurredAt` (DateTime) as an identical BSON UTC datetime in both. **`SerializeConstant`'s claim holds** — no bug found, nothing to report under the "stop and report" escalation path.

## What the missing-field self-referencing setter actually wrote

Confirmed by reading the raw document back after the update: the `Quantity` field, absent from the seeded raw document, is present after `ExecuteUpdate(s => s.SetProperty(o => o.Quantity, o => o.Quantity + 1))` and its `BsonType` is `Null`. This **matches the brief's prediction exactly** ("$add over a missing field evaluates to null, so the property is set to null rather than left alone"). No test rename was needed for this one — the brief's chosen name was already accurate; what was missing was the assertion that actually confirms it.

## An unrelated finding surfaced along the way (fixed at the test-model level, not in `src/`)

Adding `RichOrder` with `OwnsOne(o => o.Shipping)` initially broke **all four new tests on EF9** (not EF10) with:

```
System.InvalidOperationException: The 'Select' operator is not supported in a bulk delete or update. ...
```

Root cause: EF Core auto-includes owned navigations by convention, and on EF9 that auto-include survives into the `ExecuteUpdate` source query as a `Select(Include(...))` wrapper. `ClassifyBulkSource` doesn't recognize `Select` as one of the operators that can scope a bulk operation (`Where`/`OrderBy`/`Take`/etc.), so it rejects the query outright — for **every** setter, not just the representation-sensitive ones, and regardless of `SerializeConstant`. EF10 does not exhibit this (the auto-include apparently doesn't reach the bulk source the same way there), so the divide is EF9-specific.

This is a real, previously-undiscovered gap in bulk-operation support for entities with owned navigations on EF9 — no existing test in the repo combines `OwnsOne`/`OwnsMany` with `ExecuteUpdate`/`ExecuteDelete` (checked via grep across `FunctionalTests`). I did **not** fix it in `src/` — the task's global constraint restricts `src/` changes to the one named exception (a `SerializeConstant` mismatch), and this isn't that. Instead I added `mb.Entity<RichOrder>().Navigation(o => o.Shipping).AutoInclude(false);` to `ConfigureRichModel`, which sidesteps the auto-include and makes all four tests pass on both EF9 and EF10 without touching production code. This keeps the setter-shape tests focused on what they're meant to test (not owned-navigation loading) but it also means **this task's tests do not exercise or pin down the underlying EF9 auto-include/bulk-source interaction** — that gap is now known but not covered by any test. Flagging this explicitly: **a future task should decide whether `ClassifyBulkSource` needs to tolerate a benign `Select(Include(...))` auto-include wrapper on EF9, and add a dedicated test for it** (distinct from this task's setter-shape tests). I did not expand this task's scope to fix or test that gap myself, per the "don't let it expand the task" guidance and the "no `src/` changes expected" constraint.

## Task 9 MemberInfo/name-divergence test — not added

I did not add a test for the open "MemberInfo/name divergence" gap noted in Task 9's ledger. Reasoning: `IEntityType.FindProperty(string)` can only ever return the single property mapped under that name — an entity type's metadata is keyed by name, so there is no way for two *different* mapped properties on the *same* entity type to collide by name and produce an actual wrong-property match observable through a setter selector. The only way to construct a genuine divergence (a case where `FindProperty(MemberInfo)` and `FindProperty(member.Member.Name)` would disagree) is a CLR-level shape outside this file's existing flat-POCO/simple-TPH style — e.g. inheritance property-hiding via `new` (an obscure, undocumented-here EF feature) or an explicitly-implemented interface member (which, when selected directly, doesn't reach the member branch with a colliding plain name at all). Task 9's own report explicitly declined to add this for the same reason ("needs model shapes... no additional case requested"). Constructing a contrived model shape here risked producing exactly the anti-pattern this task exists to correct — a test whose name promises more than it verifies. Per the brief's own escape valve ("If it needs model shapes that don't fit this file, leave it and note it"), I left it and am noting it here.

## What would have to break for each test to fail

- `ExecuteUpdate_setting_null_writes_null`: a regression that drops a `null` constant from the `$set` document, coerces it to something else, or fails to translate a nullable-reference-typed constant setter.
- `ExecuteUpdate_setting_enum_Guid_and_DateTime_writes_the_same_BSON_as_SaveChanges`: any divergence between the bulk-setter serialization path (`SerializeConstant` → `BsonSerializerFactory.GetPropertySerializationInfo(...).SerializeValue(...)`) and the ordinary `SaveChanges` write path for an enum, a Guid, or a DateTime — e.g. enum-as-string vs. int, Guid-as-string vs. binary (or a different GUID representation/byte order), or a DateTime precision/kind mismatch. Also fails if the EF-mediated read-back of the bulk-updated value diverges from what was set.
- `ExecuteUpdate_targeting_an_owned_nested_property_throws`: a regression in `BuildSetter`'s owned/nested-target rejection (message text or absence of a throw).
- `ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null`: any change to how `$add` (or whatever renders the self-referencing aggregation expression) handles a missing field — e.g. if it started leaving the field absent, or writing `0`/`1` instead of `null`.

## Testing performed

All runs with `MONGODB_URI` and `ATLAS_URI` unset (TestContainers atlas-local, confirmed via `echo` before running).

```
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"   → Build succeeded, 0/0
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF9"    → Build succeeded, 0/0
dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF8"    → Build succeeded, 0/0

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~ExecuteUpdateTests"
  → Passed: 49, Failed: 0

dotnet test ... -c "Debug EF9" --filter "FullyQualifiedName~ExecuteUpdateTests"
  → Passed: 50, Failed: 0   (the extra one is the EF9-only ExecuteUpdate_with_no_set_property_throws)

dotnet test ... -c "Debug EF10" --filter "FullyQualifiedName~NorthwindBulkUpdatesMongoTest"
  → Passed: 181, Failed: 0   (baseline confirmed, unchanged)
```

Iteration note: the first EF9 run of the four new tests failed 4/4 with the owned-navigation `Select` issue described above; after adding `AutoInclude(false)` to `ConfigureRichModel`, all four passed on EF9 and the full `ExecuteUpdateTests` + Northwind suites were re-run clean on both EF9 and EF10 as shown above.

## Files changed

- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` (+168 lines; no other files touched)

## Self-review

- Each new test's name matches exactly what it asserts (see "what would have to break" above for each).
- `ExecuteUpdate_setting_enum_Guid_and_DateTime_writes_the_same_BSON_as_SaveChanges` was renamed from the brief's suggestion because the original name promised a SaveChanges comparison it didn't perform; the new name is now literally true.
- `ExecuteUpdate_self_referencing_on_a_field_absent_from_the_document_writes_null`'s name was kept as-is since the observed behavior matches it, but the test body was changed to actually verify it.
- Test output is pristine: no stray `Console.WriteLine`, no leftover probe code (added and removed within this session, confirmed by re-running the full suite clean afterward), no skipped/pending tests.
- No BOM was present in the original file, so none needed preserving; verified the file still starts with `/* Copyright...` (no BOM introduced).

## Concerns

- The EF9 owned-navigation/`Select` gap (see above) is real and un-pinned by any test. It's out of this task's scope per the brief and the "no `src/` changes" constraint, but it should be tracked — recommend a follow-up task/ticket.
- The Task 9 MemberInfo/name-divergence gap remains open and untested, for the reasons given above.

## Status

DONE_WITH_CONCERNS — all four tests added and pass on both EF9 and EF10, all three builds are green, Northwind baseline (181/181) is unchanged. Concerns are the two open gaps noted above (EF9 owned-navigation bulk-source classification; Task 9's MemberInfo/name-divergence coverage), neither of which this task's brief asked to be fixed, and both of which are explicitly flagged rather than silently left.
