# Complex types: mutation-discrimination table (Task 15)

Branch `EF-168a`. Every guard/decline added in Tasks 4-14 that a task report listed with a mutation, consolidated, plus a
re-verification at HEAD of a risk-ranked sample (every wrong-rows/data-loss guard class named in the Task 15 brief). Counts in
this document are produced by the script that wrote it from the result files, never restated by hand.

Legend: **caught** (a test fails with the mutation), **equivalent** (no behaviour difference is possible; reason given),
**survived** (no test fails; reason given). *Re-verified at HEAD* rows were applied, built and run sequentially in this task
(`/tmp/ef168a-task15/scripts/mut.py`; each file restored by `git checkout` and `git diff --quiet -- src` asserted after each).
Unit + Functional filter for EF10 rows: the complex/owned/join/include/structural-path area (Functional ~1888 tests, Unit ~399);
survivors were re-run against the FULL Unit (1872) and Functional (7195) suites.

## A. Re-verified at HEAD (EF10)

| ID | Task | Guard / decline (file ~line, what it does) | Mutation | Result | Failing tests (first three) | How verified |
|---|---|---|---|---|---|---|
| B1 | 14 | bulk refusal entry `ThrowIfBulkMisreadsNullElements` (ComplexElementNullGuardRefusal.cs ~109): refuse unsafe bulk before any write | refusal never fires | caught | `A_parameter_is_checked_per_execution`; `A_query_filter_with_an_unsafe_element_predicate_is_refused_unless_ignored`; `A_two_phase_update_whose_setter_value_reads_elements_unsafely_writes_nothing` (14 failing) | re-verified at HEAD |
| B2 | 14 | `IsBulkSafeOperator` optional-collection arm (~541) | optional collections admitted | caught | `Operators_over_an_optional_complex_collection_are_refused` (1 failing) | re-verified at HEAD |
| B3 | 14 | `IsBulkSafeOperator` CLR-type normalisation arm `IsNormalizableToEmptyList` (~549) | T[]/ObservableCollection admitted | caught | `Operators_over_an_ObservableCollection_typed_collection_are_refused_and_write_nothing`; `Operators_over_an_array_typed_collection_are_refused_and_write_nothing`; `Bulk_refuses_operators_over_collection_types_the_bridge_cannot_normalise` (3 failing) | re-verified at HEAD |
| B4 | 14 | `IsBulkSafeOperator` operator allow-list (~556) | every operator admitted | caught | `Setter_values_that_read_elements_are_checked_like_filters`; `Shapes_outside_the_allow_list_are_refused`; `Shapes_the_query_declines_are_refused_too` (6 failing) | re-verified at HEAD |
| B5 | 14 | element predicate must satisfy `IsBulkSafePredicate` (~303) | predicate check skipped | caught | `A_parameter_is_checked_per_execution`; `A_query_filter_with_an_unsafe_element_predicate_is_refused_unless_ignored`; `A_two_phase_update_whose_setter_value_reads_elements_unsafely_writes_nothing` (19 failing) | re-verified at HEAD |
| B6 | 14 | `IsSafeEqualityValue` bool `== false` exclusion (R19, ~612) | bool equality with any value admitted | caught | `Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused`; `Bulk_allow_list_refuses_everything_else` (3 failing) | re-verified at HEAD |
| B7 | 14 | collection read outside an approved operator, METHOD-CALL arm (EF.Property spelling, ~312) | check removed (member arm kept) | survived → caught after new tests | survived the full suites (only the member-arm spelling was pinned); added unit row `EF.Property collection read outside an operator` and a functional ExecuteDelete row in `Shapes_the_query_declines_are_refused_too`; both fail under the mutation (re-run B7-recheck, B7-func: caught) | re-verified at HEAD |
| B8 | 14 | `list.Contains(member)` only for lists that cannot hold null (~598) | lists with null admitted | caught | `A_parameter_is_checked_per_execution`; `Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused`; `Bulk_allow_list_refuses_everything_else` (3 failing) | re-verified at HEAD |
| R20a | 13 | R20 query refusal: relational arm (~345) | arm off | caught | `Null_element_date_add_coalesce_and_conditional_read_null`; `Null_element_remaining_shapes_per_mode`; `Null_element_shapes_the_native_path_declines_are_refused_rather_than_served_wrong` (17 failing) | re-verified at HEAD |
| R20b | 13 | R20 query refusal: `== null`/`!= null` arm (~356) | arm off | caught | `Indexer_ElementAt_and_First_with_a_predicate_decline`; `Null_guard_requiring_shapes_in_a_complex_element_scope_are_found` (3 failing) | re-verified at HEAD |
| R20c | 13 | R20 query refusal: non-nullable bool equality arm (R19, ~363) | arm off | caught | `Null_guard_requiring_shapes_in_a_complex_element_scope_are_found` (1 failing) | re-verified at HEAD |
| R20d | 13 | R20 query refusal: local-list Contains of a member (~326) | arm off | caught | `Null_guard_requiring_shapes_in_a_complex_element_scope_are_found` (1 failing) | re-verified at HEAD |
| R20e | 13 | R20 DriverLinq opt-out `ThrowIfDriverLinqMisreadsNullElements` (~59) | refusal never fires | caught | `Indexer_ElementAt_and_First_with_a_predicate_decline`; `Null_element_date_add_coalesce_and_conditional_read_null`; `Null_element_remaining_shapes_per_mode` (7 failing) | re-verified at HEAD |
| R17 | 13 | `ScopeField` NullSafe in complex element scopes (MongoExpressionTranslator.cs ~1274) | NullSafe dropped | caught | `Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused`; `Counts_with_a_null_guard_requiring_predicate_are_refused`; `Null_tests_over_element_leaves_are_refused` (16 failing) | re-verified at HEAD |
| R18a | 13 | `MayBeNull` structural DateAdd (MongoAggregationExpressionRenderer.cs ~965) | type-only | caught | `Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused`; `Null_element_DateTimeOffset_members_read_null`; `Null_element_date_add_coalesce_and_conditional_read_null` (6 failing) | re-verified at HEAD |
| R18b | 13 | `MayBeNull` structural Coalesce (~967) | type-only | caught | `Null_element_date_add_coalesce_and_conditional_read_null`; `Coalesce_whose_both_sides_may_be_null_at_the_root_gets_the_relational_guard`; `MayBeNull_is_structural_for_date_add_coalesce_and_conditional` (3 failing) | re-verified at HEAD |
| R18c | 13 | `MayBeNull` structural Conditional (~970) | type-only | caught | `Null_element_date_add_coalesce_and_conditional_read_null`; `Coalesce_whose_both_sides_may_be_null_at_the_root_gets_the_relational_guard`; `Conditional_over_a_null_branch_at_the_root_gets_the_relational_guard` (5 failing) | re-verified at HEAD |
| R19 | 13 | `ScopeValue` reads a null element's non-nullable bool as false (~1284) | off | caught | `Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused`; `Null_element_bool_leaf_reads_false_in_every_spelling` (2 failing) | re-verified at HEAD |
| T13a | 13 | `RequiresAggregationElementScope` ($map form for complex collections, ~1265) | false ($elemMatch) | caught | `A_required_collection_under_an_optional_hop_is_served_when_the_predicate_is_safe`; `Bool_equalities_local_lists_with_null_and_date_arithmetic_are_refused`; `Bool_members_lists_and_combinations` (24 failing) | re-verified at HEAD |
| T13b | 13 | bridge `TryRewriteRequiredComplexCollectionSource` excludes OPTIONAL collections (MongoEFToLinq...cs ~962) | optional coalesced too | caught | `Optional_collection_null_check_and_quantifiers` (1 failing) | re-verified at HEAD |
| R14 | 12 | presence guard `WhenPresent` for any optional ancestor (ComplexEquality.cs ~427) | guard dropped | caught | `Single_complex_value_inside_an_element_compares_member_wise_and_a_null_element_is_absent`; `Captured_optional_instance_is_read_per_execution`; `Null_and_instance_comparisons_in_one_predicate` (7 failing) | re-verified at HEAD |
| R15 | 12 | stored-pair bijection `MapsSameMembers` (ComplexEquality.cs ~444) | check off | caught | `Stored_pair_with_different_mapped_members_declines_in_both_operand_orders` (1 failing) | re-verified at HEAD |
| T12a | 12 | stored-pair leaves must be `StoredAlike` (~548) | check off | caught | `Two_stored_values_not_stored_alike_decline`; `Value_converted_leaves_compare_stored_alike_or_decline`; `Stored_values_not_stored_alike_decline` (3 failing) | re-verified at HEAD |
| T12b | 12 | member-wise leaf NullSafe (R16 variant, ~505) | nullSafe false | caught | `Contains_of_a_captured_element_is_member_wise`; `Single_complex_value_inside_an_element_compares_member_wise_and_a_null_element_is_absent`; `Complex_null_check_and_all_null_instance_agree_inside_element_scopes` (4 failing) | re-verified at HEAD |
| J1 | 11 | inner `IsDirectKeyRead` in `LookupImplementsKeySelectors` (QMTEV ~2778) | removed | caught | `Inner_hop_key_named_like_the_navigations_principal_key_does_not_use_the_navigation` (3 failing) | re-verified at HEAD |
| J2 | 11 | outer `IsDirectKeyRead` in `TryResolveRawKeyJoinProperties` (QMTEV ~2847) | outer side removed | caught | `Join_key_through_a_complex_hop_named_like_a_navigations_foreign_key_does_not_use_the_navigation`; `Join_key_through_a_complex_hop_never_resolves_to_a_same_named_root_property`; `Join_key_through_an_owned_hop_never_resolves_to_a_same_named_root_property` (3 failing) | re-verified at HEAD |
| J3 | 11 | inner `IsDirectKeyRead` in `TryResolveRawKeyJoinProperties` (QMTEV ~2847) | inner side removed | caught | `Inner_hop_key_named_like_the_navigations_principal_key_does_not_use_the_navigation`; `Join_key_through_a_complex_hop_never_resolves_to_a_same_named_root_property` (4 failing) | re-verified at HEAD |
| J4 | 11 | `StructuralPath.TryResolveSelectorLeaf` full dotted path (StructuralPath.cs ~165) | leaf segment only | caught | `Filtered_Include_ThenBy_and_Take_through_complex_leaves_sort_by_the_leaves`; `Filtered_Include_ordered_by_a_complex_leaf_sorts_by_the_leaf_not_a_same_named_root_property`; `Whole_entity_LeftJoin_keyed_through_a_complex_hop_does_not_join_on_a_same_named_root_property` (3 failing) | re-verified at HEAD |
| R7 | 10 | `ThrowIfComplexValueOperand` (QMTEV ~223) | call removed | caught | `Operators_over_a_whole_collection_projection_are_refused`; `Operators_over_a_construction_holding_a_complex_value_are_refused_in_every_mode`; `Operators_over_a_construction_holding_an_optional_or_collection_complex_value_are_refused_in_every_mode` (66 failing) | re-verified at HEAD |
| R8 | 10 | `ThrowIfGroupByOverComplexValue` post-group Select (QMTEV ~552) | call removed | caught | `GroupBy_over_a_whole_complex_value_is_refused_in_every_mode` (2 failing) | re-verified at HEAD |
| T10a | 10 | `IsRootProjectionShaper` in `TryResolveFieldAccessSource` (PBRV ~1408) | CLR-type only | caught | `Mixed_projection_of_a_derived_reference_entity_reads_its_complex_values_from_the_joined_document` (1 failing) | re-verified at HEAD |
| T10b | 10 | `IsRootProjectionShaper` in the mixed reader (MixedPBRV ~383) | CLR-type only | caught | `Derived_outer_root_in_a_join_reads_its_own_members_from_the_root_document`; `Derived_outer_root_in_a_join_without_complex_properties` (2 failing) | re-verified at HEAD |
| T10c | 10 | materialization `Absent`: only OPTIONAL reads default (ComplexTypeMaterializationBuilder.cs ~145) | every absent value defaults | caught | `Whole_collection_beside_scalars_and_a_count`; `Whole_collection_projection_preserves_order_and_every_stored_state`; `Complex_collections_empty_null_missing_null_element_and_nested` (11 failing) | re-verified at HEAD |
| T9a | 9 | TranslationFailed net consumer in `TranslateSelect` (QMTEV ~976) | decline off | survived (unreachable today) | survived the full suites; as Task 9 recorded (P1), no current query reaches the net natively (Task 15 audit matrix `Untranslatable_or_unusual_leaf_spellings_never_read_default_rows` re-confirmed: 0 failures with the net off); the flag itself is pinned by the unit test (Task 9 P5). Kept as the safety net (ruling in Task 9 review) | re-verified at HEAD |
| T9b | 9 | read-side complex hop element name `TryResolveComplexPropertyDocument` (PBRV ~1542) | CLR name | caught | `Client_evaluated_leaf_reads_the_stored_element_name_not_the_CLR_name`; `Same_complex_clr_type_at_two_paths_with_different_element_names`; `Deepest_leaf_in_predicate_ordering_and_projection` (3 failing) | re-verified at HEAD |
| T8a | 8 | `StructuralPath.TryResolveHop` complex segment = `GetElementName()` (StructuralPath.cs ~267) | CLR name | caught | `Camel_case_convention_names_the_collection_and_element_leaves`; `Constant_setters_beside_a_self_referencing_one_are_written_as_literals`; `Converted_and_represented_leaves_are_written_in_their_stored_form` (42 failing) | re-verified at HEAD |
| T8M1 | 8 | `TryResolveHop` `!navigation.IsEmbedded()` (StructuralPath.cs ~243) (Task 8 survivor M1) | check removed | caught | `Reference_navigation_to_non_owned_target_with_element_name_annotation_declines_as_hop_and_leaf` (1 failing) | re-verified at HEAD |
| T7 | 7 | `GetComplexPropertySerializationInfo` element name (BsonSerializerFactory.cs ~130) | CLR name | caught | `Camel_case_convention_names_the_collection_and_element_leaves`; `Same_element_type_on_two_collections_with_different_element_names`; `ExecuteDelete_and_ExecuteUpdate_filter_on_complex_leaves` (42 failing) | re-verified at HEAD |
| W1 | 5/6 | writer: rewrite a changed complex property whole on update (ComplexValueWriter.cs ~124) | skip every complex property on update | caught | `Query_loaded_complex_collection_element_change_rewrites_the_whole_array`; `Query_loaded_entity_leaf_change_is_detected_and_saved_as_a_whole_subdocument`; `Collection_nested_in_collection_element_change_rewrites_whole_outer_array` (34 failing) | re-verified at HEAD |
| W2 | 5/6 | writer: skip unchanged complex properties on update (~124) | rewrite every complex property | caught | `Query_loaded_entity_leaf_change_is_detected_and_saved_as_a_whole_subdocument`; `Collection_nested_in_collection_element_change_rewrites_whole_outer_array`; `Collection_nested_in_complex_property_change_rewrites_parent_subdocument_only` (9 failing) | re-verified at HEAD |

## B. Re-verified at HEAD (EF9: EF8/EF9 code paths)

| ID | Task | Guard / mutation | Result | Failing tests (first two) | How verified |
|---|---|---|---|---|---|
| T8a-EF9 | 8 | as T8a, EF9 | caught | `Constant_setters_beside_a_self_referencing_one_are_written_as_literals`; `Converted_and_represented_leaves_are_written_in_their_stored_form` (39 failing) | re-verified at HEAD (Debug EF9) |
| J1-EF9 | 11 | as J1, EF9 | caught | `Inner_hop_key_named_like_the_navigations_principal_key_does_not_use_the_navigation` (3 failing) | re-verified at HEAD (Debug EF9) |
| T7-EF9 | 7 | as T7, EF9 | caught | `ExecuteDelete_and_ExecuteUpdate_filter_on_complex_leaves`; `ExecuteUpdateAsync_and_ExecuteDeleteAsync_over_complex_leaves` (39 failing) | re-verified at HEAD (Debug EF9) |
| W1-EF9 | 5/6 | as W1, EF9 | caught | `Query_loaded_entity_leaf_change_is_detected_and_saved_as_a_whole_subdocument`; `Same_clr_type_at_several_paths_updates_only_the_changed_path` (13 failing) | re-verified at HEAD (Debug EF9) |
| R14-EF9 | 12 | as R14, EF9 (owned optional ancestor) | caught | `Required_value_under_an_absent_optional_owner_is_not_equal_to_an_all_null_instance` (1 failing) | re-verified at HEAD (Debug EF9) |
| R7-EF9 | 10 | as R7, EF9 | caught | `Operators_over_a_construction_holding_a_complex_value_are_refused_in_every_mode`; `Operators_that_read_a_projected_complex_value_are_refused_in_every_mode` (39 failing) | re-verified at HEAD (Debug EF9) |
| W3-EF9 | 5 | writer shadow-leaf value from the entry (EF8/EF9 only, ComplexValueWriter.cs ~220): write null instead | caught | `Shadow_property_on_complex_type` (1 failing) | re-verified at HEAD (Debug EF9) |

## C. Consolidated from the task reports (verified by the report; "→ A/B" = also re-verified at HEAD above)

| Task | ID (report) | Guard / decline (what it does) | Result | Test(s) / reason | How verified |
|---|---|---|---|---|---|
| 4 | 1 | `writeAll` on insert (Added writes every complex property) | caught | every insert test | task-4 report |
| 4 | 6 | `IsEmbeddedCollection()` array branch in `WriteComplexValue` | caught | EF10 order/empty write tests | task-4 report |
| 4 | 8 | leaf element name from serialization info | caught | HasElementName write test | task-4 report |
| 5 | 1-3 | `writeAll`, `!writeAll && !AnyLeafPasses` skip, nested `AnyLeafPasses` recursion | caught | scalar-update `$set` test; City/Lat/replace/attach; nested-struct test | task-5 report (→ A: W1, W2) |
| 5 | 5 | empty-element-name skip in `WriteSubdocument` | caught (insert only) | `Complex_leaf_with_empty_element_name_is_not_written` | task-5 report |
| 5 | 6 | shadow-leaf value read from the entry (EF8/EF9) | caught | `Shadow_property_on_complex_type` | task-5 report (→ B: W3-EF9) |
| 5 | 10 | `ElementPath?.Last()` branch | equivalent | ElementPath is always null for complex leaves (dead branch) | task-5 report |
| 6 | 1, 2 | `IsChanged` collection branch `IsModified(IComplexProperty)` (drop / over-write) | caught | 13 / 4 tests | task-6 report |
| 6 | 3 | `IsChanged` nested recursion | caught | 3 tests | task-6 report |
| 6 | 4 | `onlyModified` excludes the owned `_ => true` path | caught | 2 tests | task-6 report |
| 6 | 5, 6 | null value / null element → `WriteNull` | caught | 6 / 2 tests | task-6 report |
| 6 | 7 | `onlyModified && !IsChanged` skip | caught | 22 tests | task-6 report (→ A: W1, W2) |
| 7 | M1 | serializer element name = `GetElementName()` | caught | 4 tests | task-7 report (→ A: T7; B: T7-EF9) |
| 7 | M2 | nominal type = `complexProperty.ClrType` | caught | 2 (collection, optional struct) | task-7 report |
| 7 | M3, M4 | nested `FindComplexProperty` / leaf `FindProperty` in `ComplexTypeSerializer` | caught | 1 / 12 | task-7 report |
| 7 | M5, M9 | `IsEmbeddedCollection()` / `type.IsArray` branches | caught | 2 / 1 | task-7 report |
| 7 | M6 | `WrapNullable` for optional struct | caught | 1 | task-7 report |
| 7 | M7 | `EntitySerializer` complex lookup | caught | 18 | task-7 report |
| 7 | M8 | property-level serializer cache | caught (after a test was added) | `Factory_caches_complex_collection_serializer` | task-7 report |
| 7 | M10 | type cache keyed by `IReadOnlyComplexType` (not CLR type) | caught | 2 (same CLR type at two paths) | task-7 report |
| 7 | M11 | `Serialize` throws (ruling R1) | caught | 1 | task-7 report |
| 8 | M1 | hop `!navigation.IsEmbedded()` | survived at Task 8 → caught (Task 9 added the annotation test) | `Reference_navigation_to_non_owned_target_with_element_name_annotation_declines_as_hop_and_leaf` | task-8/9 reports (→ A: T8M1, re-confirmed caught) |
| 8 | M2, M3 | owned / complex `IsCollection` hop → crossing | caught | owned/complex collection hop tests | task-8 report |
| 8 | M4 | composite `_id.` leaf path | caught | composite-key leaf tests | task-8 report |
| 8 | M5, M6 | complex segment `GetElementName()` / owned `GetContainingElementName()` | caught | override, two-path, camel-case, mixed chain | task-8 report (→ A: T8a; B: T8a-EF9) |
| 8 | M7, M8 | `CrossesCollection` only for collections; nav leaf must be embedded | caught | reference-navigation test | task-8 report |
| 8 | M9, M10 | entity-operand / collection-path leaf navigation kinds | caught (after tests added) | `Translator_does_not_treat_owned_collection_as_entity_typed_null_operand`; `Translator_resolves_owned_collection_array_but_not_owned_reference` | task-8 report |
| 8 | M11, M12 | `LeafOwner`; field path joins all segments | caught | 6 / 9 | task-8 report |
| 9 | MA | read-side `TryResolveComplexPropertyDocument` resolves the hop | caught | 3 | task-9 report |
| 9 | MC, N5 | gate 1f dotted leaf → `_v` | caught | 13 / 19 | task-9 report |
| 9 | MD, P4 | required reference-typed Distinct marker | caught | 3; `Distinct_over_a_required_byte_array_or_string_list_matches_main` | task-9 report |
| 9 | ME | bridge `WalkMember` complex branch | caught | converted-leaf decline | task-9 report |
| 9 | MF | bridge `StoredOrderingPropertyFinder.VisitMember` complex check | survived | defensive mirror; reached only by shapes the walker can't classify (none found) | task-9 report |
| 9 | MG / T5(10) | read-side element name (CLR-name decoy) | survived at Task 9 → caught at Task 10 | decoy test `Client_evaluated_leaf_reads_the_stored_element_name_not_the_CLR_name` | task-9/10 reports (→ A: T9b) |
| 9 | N1, N2 | EF.Property complex mirror (read / binding) | caught | `efprop_hop_*` rows | task-9 report |
| 9 | N3 | gate 1f `HasDefaultKeySerialization` conjunct | equivalent | the earlier `TryTranslateLeaf` converted-dotted decline handles the same rows | task-9 report |
| 9 | N4 | marker `== typeof(string)` | equivalent at the time (scope limit); superseded by P4 | — | task-9 report |
| 9 | P2/P3 | hop whole-leaf arm; arm + TranslationFailed decline | caught | 14 (P3 contrast proves the net prevents null rows) | task-9 report |
| 9 | P1 | `TranslateSelect` TranslationFailed decline alone | survived (unreachable) | no shape reaches the net natively; flag pinned by P5 | task-9 report (→ A: T9a, re-confirmed) |
| 9 | P5 | `MatchTypes` sets the flag | caught | unit test | task-9 report |
| 10 | T1-T4 | builder: missing/null required → throw; optional → null; wrong BSON type → FormatException | caught | 4 / struct+null / optional+collection / Wrong_bson_type | task-10 report (→ A: T10c) |
| 10 | T5-T7 | DOM marker / one-pass dispatch / nested element names | caught | camel-case, two-paths, decoy | task-10 report |
| 10 | T8 | one-pass resets complex locals per pass | caught | owned-collection element test | task-10 report |
| 10 | T9, T11, T12, T14, T15 | complex-value decline; flag; gate 1g; marking pre-pass; read-side wrap | caught | 2 / 1 / 5 / 29 / 27 | task-10 report |
| 10 | T10 | `IsPlainProjectedSelect` conjunct | survived alone (defence in depth; T9+T10 together caught) | first gate covers it | task-10 report |
| 10 | T13 | derived-shaper resolution | caught | TPH fallback | task-10 report |
| 10 | T16 | leftover complex-leaf read guard | unreachable | EF rejects the model | task-10 report |
| 10 | R7a-d | R7 refusal call / flag / depth / value-free carve-out | caught | 17 / 17 / 4 / 1 | task-10 report (→ A: R7; B: R7-EF9) |
| 10 | J1, J2 | derived-shaper arm placement / removal | caught | derived-reference test; TPH | task-10 report |
| 10 | M1, M3, M4-M7 | shared predicate wrapper; positional-ctor decline; root-shaper fixes; finder stop | caught | 67 / 1 / 3 / 92 | task-10 report (→ A: T10a, T10b) |
| 10 | M2 | native-flag disjunct | survived (redundant; kept so both flags come from one predicate) | — | task-10 report |
| 10 | G1-G4, F1 | GroupBy refusals; leaf-receiver skips; finder unknown nodes | caught | key/anon key; grouped select; leaf GroupBy; unit | task-10 report (→ A: R8) |
| 10 | G5 | `IsRootProjectionShaper` own-query check | survived (no reachable shape; defensive) | — | task-10 report |
| 10 | M-SM1 | SelectMany declaring-type guard | caught | 1 | task-10 report |
| 10 | M-C2 | `IsRootProjectionShaper` removed from both root arms | caught (2 R9 tests); the 9 C2 pins stay green by design (unreachable for a served row) | — | task-10 report (→ A: T10a/T10b) |
| 11 | M1, M2, M4, M6, M7, M10 | bridge join complex arms; leaf-name key path; `_outer` redirects; anchor search | caught | 15 / 1 / 1 / 1 / 1 / 1 | task-11 report |
| 11 | M3 | `IsDirectKeyRead` → true | caught | 3 | task-11 report (→ A: J1-J3; B: J1-EF9) |
| 11 | M9 | inner `IsDirectKeyRead` in `LookupImplementsKeySelectors` | survived at first → caught after I1 tests | 3 theory rows | task-11 report (→ A: J1) |
| 11 | M11, M12 | Include sort key / shared `TryResolveSelectorLeaf` by leaf name only | caught | sort test; LeftJoin hop + sort | task-11 report (→ A: J4) |
| 12 | M1-M15 | complex equality (presence, null forms, `$ifNull`, runtime branch, StoredAlike, collection-leaf guard, scope identity, flip, HasValue, `!(a==b)`, per-execution values, `!=` form) | 14 caught, 1 equivalent (M2: null serializes as BSON null; branch deleted) | see report | task-12 report (→ A: R14, T12a) |
| 12 | F1-F7 | owned/complex absent marking; bijection; leaf NullSafe; ancestor flag; collection-leaf guard; default struct | caught | see report | task-12 report (→ A: R15, T12b; B: R14-EF9) |
| 12 | G1, G4 | stored-pair flag leak; right-side optionality | caught | 4 matrix cells; optional-intermediate test | task-12 report |
| 12 | G2, G3 | `with` drop; redundant nested rebuild | equivalent | resolver already folds optionality / change removed | task-12 report |
| 13 | M1, M2, M3, M5, M7, M8, M9, M12, M13 | `$map` form; element absent; bridge normalisation; EF-337 arm; `!Contains`; `IsNull("")`; collection resolver; optional coalesce; owned stays `$elemMatch` | caught | see report | task-13 report (→ A: T13a, T13b) |
| 13 | M6 | `IsFoldSafe` complex guard | equivalent (guard deleted) | — | task-13 report |
| 13 | M10, M11 | correlated-scope guard; Contains arm scope | survived (equivalent today; defensive) | see report | task-13 report |
| 13 | R17a, R17b | `ScopeField` complex-scope NullSafe; bridge rewrite | caught | 5 / 22 | task-13 report (→ A: R17) |
| 13 | R2a-c | `RenderIn` null-safe; `UtcDateTime` NullSafe; DTO-local MayBeNull | caught | see report | task-13 report |
| 13 | R3a-e | MayBeNull DateAdd/Coalesce/Conditional; `ScopeValue`; prefix rewriter NullSafe | caught (R3e after a unit test) | see report | task-13 report (→ A: R18a-c, R19) |
| 13 | M-R4a-c | R20 refusal off; DriverLinq opt-out; complex-only keying | caught | 5+14 / 17+1 / owned control+2 | task-13 report (→ A: R20a-e) |
| 13 | M1 (r5) | `ParameterReadFinder` stops at nested lambdas | caught | `Relational_over_a_nested_element_lambda_count_is_not_refused` | task-13 report |
| 14 | M1-M9, M11-M16 | bulk refusal, opt-out, element-predicate check, atoms, optional collections, operator list, collection-read (member arm), setters, `$literal`, stored path, two-phase check | caught | see report | task-14 report (→ A: B1-B8) |
| 14 | M10 | unbound element lambda check | caught (unit only; functionally unreachable) | unit row | task-14 report |
| 14 | N1-N3, N5 | CLR-type normalisation refusal; unknown-extension refusal; BsonRepresentation leaf refusal; single-command scan | caught | see report | task-14 report (→ A: B3) |
| 14 | N4 | same CLR-type condition on `.Count` member arm | not caught → addition reverted (no dead guard kept) | EF lowers `.Count` to `Count()` | task-14 report |
| 15 | B7 | bulk collection read outside an operator, METHOD-CALL (EF.Property) arm | survived → caught after the Task 15 tests | unit `EF.Property collection read outside an operator`; functional `Shapes_the_query_declines_are_refused_too` | this task (A: B7) |

## Summary (counted from the rows above by the generating script)

- Section A (EF10, re-verified at HEAD): 40 mutations; results: {'caught': 38, 'survived → caught after new tests': 1, 'survived (unreachable today)': 1}.
- Section B (EF9, re-verified at HEAD): 7 mutations; results: {'caught': 7}.
- Section C (consolidated from the reports): 76 rows (several rows group a report's IDs).
- Findings from the re-verification: **B7** (a real gap: the EF.Property spelling of a bulk collection read outside an operator
  was unpinned; now pinned by a unit row and a functional ExecuteDelete row, both RED under the mutation) and **T9a** (known
  Task 9 P1 survivor: unreachable today, flag pinned by unit P5).
