# Iteration 141 — handwritten test and correction plan

1. Add a handwritten scalar/task ownership fixture and unique CoreRequirements bindings before changing productive wait semantics. Freshly compile and run it against the defective waits; record behavioral failures separately from authoring/compiler failures.
2. Exercise direct and converting variable adapters across accepted variable and downstream-converter stages, each with success, exact ordinary fault and producer cancellation. Assert exact context/input/token, pending ownership after caller cancellation and the original outcome.
3. Exercise converting nullable and the task adapter's accepted downstream conversion across the same three outcomes. For the task adapter, complete the caller-owned source first, wait until downstream acceptance, then cancel the caller.
4. Preserve the intentional boundary: direct and converting task adapters may cancel while waiting for caller-owned task input, must not start downstream conversion, and must not complete the input task. Message-data conversion retains the same local-cancellation behavior for its task-valued input. Cleanup settles and observes every controlled input.
5. Prove reverse task wrapping preserves the exact accepted conversion task as its result value and retain exact null-task diagnostics.
6. Apply the smallest productive correction: directly await only accepted variable/downstream converter tasks. Retain `WaitAsync` only at caller-owned task-input boundaries, plus every existing pre-start cancellation, null/default and exact diagnostic behavior.
7. Re-run focused tests after a fresh compile. Re-open all assertions against final source, apply assertion-quality and pseudo-mutation analysis, and close every concrete gap before mutation evidence.
8. Hand-author compiled single-cause mutations for each corrected accepted wait and for each deliberate caller-owned wait boundary. Every correction mutation must fail unchanged ownership tests; removing caller-local cancellation must fail boundary tests. Restore frozen bytes after every attempt.
9. Run repository-prescribed warning-as-error/no-restore Release builds, both formatting gates, the complete unfiltered native Core profile, exact parent-multiset reconciliation and selected coverage without presenting it as whole-fork coverage.
10. Update admissions, status and detailed evidence; review the complete non-protected diff; commit normally; create an annotated Iteration-141 tag; atomically push branch and tag without force; independently verify remote branch, tag object and peeled commit; then continue the original goal.

## Requirement-to-test map

| Requirement | Planned method |
| --- | --- |
| Direct variable conversion owns its accepted value task | `DirectVariableConverter_OwnsAcceptedValueOutcomeAsync` |
| Converted variable conversion owns both accepted stages | `ConvertedVariableConverter_OwnsEachAcceptedStageAsync` |
| Nullable conversion owns the accepted underlying conversion | `ToNullableConverter_OwnsAcceptedConversionOutcomeAsync` |
| Task conversion owns downstream work after caller-owned input succeeds | `ConvertingTaskAdapter_OwnsAcceptedDownstreamOutcomeAsync` |
| Task-valued inputs remain caller-owned and locally cancellable | `TaskInputs_RemainCallerOwnedAndLocallyCancellableAsync` |
| Message-data value input remains caller-owned and locally cancellable | `MessageDataValueInput_RemainsCallerOwnedAndLocallyCancellableAsync` |
| Reverse task wrapping preserves the exact inner conversion task | `TaskWrapping_PreservesTheAcceptedConversionTaskIdentityAsync` |
| Required dependencies and accepted task results are non-null | `ScalarOwnershipConverters_RejectMissingDependenciesAndAcceptedTasksAsync` |

Every held original is independently released and observed in `finally`. Bounded pending checks require actual incompletion. Case counts and coverage percentages remain evidence summaries, not substitutes for assertions.
