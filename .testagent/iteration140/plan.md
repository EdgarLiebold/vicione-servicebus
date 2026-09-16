# Iteration 140 — handwritten test and correction plan

1. Add only the internal nested-initializer injection seam needed for deterministic testing; keep both parameterless production paths cache-backed. Then add handwritten ownership tests and CoreRequirements bindings before changing wait semantics. Freshly compile and run the focused tests against the defective waits, recording behavioral failures separately from compilation failures.
2. Exercise typed and runtime nested conversion across success, ordinary fault, and producer cancellation. Assert exact input, runtime type selection, child context, forwarded token, pending ownership after caller cancellation, and original terminal outcome.
3. Exercise every converting collection shape: array; five list contracts; four value-dictionary contracts; four key-dictionary contracts; and four combined-dictionary contracts. Use independently controlled element tasks and disposal-tracking enumerators. Assert values, tokens, exact exceptions/cancellation, pending roots, and disposal only after accepted work settles.
4. Prove the combined key/value entry owns both siblings, preserves key-first failure/cancellation priority, and drains an accepted key when value invocation throws synchronously or returns null. Prove caller cancellation stops every converter family before a second conversion begins.
5. Apply the smallest productive correction: directly await accepted nested/core/element tasks; capture both combined entry task handles; drain both with deterministic key-first priority; retain all pre-start cancellation checks, null diagnostics, shapes, and public signatures. Update only comments that need to describe accepted-task ownership.
6. Re-run focused tests after a fresh compiler. Re-open every changed assertion against final source, invoke assertion-quality and pseudo-mutation analysis, and close every concrete gap.
7. Hand-author compiled, single-cause mutations for nested wait ownership, each distinct collection wait family, shape adapters, combined sibling draining, value-invocation capture, and stop-before-next behavior. Each mutation must fail unchanged relevant tests, restore frozen bytes, and leave no residue.
8. Run warning-as-error, no-restore Release builds for product and unit solutions, following the repository rule not to combine solution builds with `--no-incremental`. Then run the complete unfiltered native Core profile and exact multiset reconciliation against Iteration 139. Collect selected coverage and record open whole-fork coverage/CRAP and real-provider gates without overstating them.
9. Update source/test admissions, status, and detailed evidence; review the complete non-protected diff; commit normally; create an annotated Iteration-140 tag; atomically push branch and tag without force; independently verify remote branch, tag object, and peeled tag; then continue to the next connected packet.

## Requirement-to-test map

| Requirement | Planned method |
| --- | --- |
| Typed and runtime nested converters own accepted initializer outcomes | `NestedConverters_OwnAcceptedInitializerAndOriginalOutcomeAsync` |
| Array, list, dictionary-value, and dictionary-key shapes own accepted conversion outcomes and enumerators | `SingleConversionCollections_OwnAcceptedTaskAndEnumeratorAsync` |
| Every combined dictionary shape owns its pending value conversion | `CombinedDictionaryShapes_OwnAcceptedValueOutcomeAsync` |
| Combined key/value conversion drains both siblings and preserves key-first priority | `CombinedDictionary_OwnsBothAcceptedTasksWithKeyFirstPriorityAsync` |
| Synchronous/null value invocation cannot abandon an accepted key task | `CombinedDictionary_ValueInvocationFailureStillOwnsAcceptedKeyAsync` |
| Caller cancellation prevents a second collection conversion without abandoning the first | `CollectionConverters_StopBeforeStartingAnotherConversionAsync` |

All controlled originals are independently released and observed in `finally` blocks. Bounded pending checks require actual incompletion. Exact case counts and coverage percentages are evidence summaries, never substitutes for behavioral assertions.
