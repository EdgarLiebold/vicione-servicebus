# Iteration 138 — handwritten test and correction plan

1. Before productive changes, add InitializerProviderLifetimeTests and additive exact requirement bindings. Compile freshly and execute only that fully qualified fixture with strict zero-test, minimum-count, skip/warn and timeout policies. Record which assertions fail against unchanged parent source. A compiler failure is not a causal behavioral witness and must not execute stale DLLs.
2. Correct only the four owning files after understanding each implementation and all comments. Directly await provider-owned outer tasks and accepted converter tasks; preserve caller-owned inner value waits, exact exported task identities, null/default semantics, argument ordering and accepted-stage continuation. Manually write functional English ownership comments, never process/provenance prose in source. Remove private cancellation parameters that become unused.
3. Replace the one existing provider-assignment abandonment assertion with original-task cooperative settlement and independently observed cleanup; preserve the existing method/case and its other assertions.
4. Run the complete focused fixture after a fresh clean compiler. Re-open every handwritten assertion against source. Obtain authorized internal static counterreview; it is not external acceptance. Apply assertion-quality and pseudo-mutation gap analysis with full source reads retained, recording dispositions in status.md.
5. Handwrite selected single-cause productive mutations only. Each must compile freshly, execute the unchanged complete focused multiset, fail on an exact behavioral witness and restore all frozen source/test/ledger bytes before the next attempt. Do not count compile errors or skipped cases as mutation kills.
6. Fresh post-restoration warning-as-error compiler and unfiltered Core native coverage; retain all 4545 parent names/multiplicities plus exactly the new focused cases. Reconcile selected nine-module counters with root and state full-fork/CRAP/external/API limits explicitly. Broader product build/pack and all-fork gates remain part of the original goal, not replaced by this run.
7. Write a detailed manual English iteration report, check frozen staged/committed/working bytes and whitespace, normally commit and create a new annotated tag, perform approved atomic normal private branch/tag push and independently verify exactly three remote refs. Report completed scope and continue the connected scalar/converter queue.

## Exact requirement-to-test map

| Requirement | Planned method |
| --- | --- |
| Three assignment stages own held provider original outcomes and distinguish caller/provider tokens | Initializers_OwnHeldProviderAndOriginalOutcomeAsync |
| Three stages reject invalid context/destination synchronously and do not start on precancellation | Initializers_ValidateArgumentsAndPreCancellationBeforeProviderEffectsAsync |
| Three stages preserve immediate success/null/fault/canceled and null-task diagnostics | Initializers_PreserveImmediateOutcomesAndNullTaskDiagnosticsAsync |
| Both async adapters own held outer tasks, default absent inner values and retain accepted-stage outcomes | AsyncAdapters_OwnOuterProviderAndOriginalOutcomeAsync |
| Both async adapters cancel caller-owned inner waits promptly without settling the input | AsyncAdapters_KeepCallerOwnedInnerValuesLocallyCancellableAsync |
| Converting async adapter owns held converter and forwards exact context/value/token | AsyncConverter_OwnsHeldConversionAndOriginalOutcomeAsync |
| Cancellation during held outer resolution still starts and owns an accepted converter with the exact canceled caller token | AsyncConverter_OwnsHeldConversionAfterCanceledOuterResolutionAsync |
| TaskPropertyProvider transfers the exact original task without awaiting/canceling it | TaskAdapter_ExportsExactOriginalOperationWithoutAwaitingAsync |
| Both async adapters avoid dependencies for absent input and precancellation | AsyncAdapters_PreserveAbsentInputAndPreCancellationEffectsAsync |
| Null outer/converter tasks retain exact stage diagnostics | AsyncAdapters_RejectNullOuterAndConverterTasksAsync |
| Constructors/context guards precede collaborator effects and cancellation | AsyncAdapters_ValidateConstructionAndContextBeforeDependenciesAsync |
| Runtime message type ownership remains enforced after held resolution | PropertyAssignment_PreservesRuntimeTypeOwnershipAfterHeldResolutionAsync |

Controlled originals are independently released/observed in finally blocks; exact exception, token, context, input, assignment and task status are asserted. Parameterization is runtime test data, not an authoring generator. No percentages or certification are inferred from case count or static pairing.
