# Iteration 140 — validation and test-quality dispositions

## Current bounded result

The original whole-fork A+ source/API/architecture objective remains active. This packet corrects typed and runtime nested initialization plus array, list, dictionary-value, dictionary-key and combined key/value conversion chains. Every accepted collaborator task is now observed to its producer-owned terminal outcome. Shape adapters cannot detach from an accepted core traversal, enumerators remain owned until accepted work settles, and a combined dictionary entry observes both accepted siblings while preserving key-first result priority. Pre-start cancellation, null diagnostics, collection shapes and existing public behavior remain intact.

No public signature, dependency, target framework or supported feature changes. The only new constructor/resolver seams are on internal converter types and preserve cache-backed parameterless production construction.

The exact Iteration-140 source admission contains 10 fully read files / 2,603 current lines and SHA-256 `ecc12b5679261eac050858b285960e6896bb4b96c2796c25da392c5180072866`. The owning-test/support admission contains 5 fully read files / 1,737 current lines and SHA-256 `ab4778bd9f46521d6034c42139e9b27afe3f26afc5d01bf605cf581523bfe3bf`. Chaining those manifests to the Iteration-139 chain hashes yields source chain hash `fbdcae2052a52091b2881d603e2fbad64df69d8a7d08163cdd528c1c97ee7247` and test chain hash `92ae6d91e60ea9f076f1810173530e73753c880dd3c8fafc85ee27655867489f`.

## Causal baseline and correction

The handwritten PropertyConverterOwnershipTests fixture contributes seven methods / 76 native cases. After only the non-semantic nested-initializer injection seam was compiled, the unchanged `WaitAsync` and sibling-abandonment implementation produced a 72-case causal baseline of 1 passed / 71 failed. The one passing dependency guard did not exercise corrected wait behavior. A separate initial compiler failure caused by test-authoring errors was fixed and is not counted as behavioral evidence.

The first corrected fixture passed 72/72. Assertion and pseudo-mutation review added exact child-context identity, nested null-task coverage and the synchronous/null value-invocation key-priority matrix. The final fixture then passed 76/76. Existing relevant contracts independently passed 25/25 before terminal whole-host validation. CoreRequirements.json retains the exact 3,009-entry parent prefix and appends seven unique bidirectional bindings, giving 3,016 entries.

## Assertion, gap and mutation analysis

All seven public test methods contain meaningful assertions. The 76 cases cover exact context/input/token forwarding, exact result and collection structure, reference and exception identity, producer cancellation tokens, pending/completed/faulted/canceled state, enumerator disposal timing/count, null injected dependencies/resolver/task results, deterministic sibling priority and absence of a second conversion. Held tasks are independently released and observed in cleanup. All Task-returning test methods have the Async suffix. Static pairing classified 87 source files and 49 tests in an isolated Initializers-only copy: 75 paired and 12 unpaired. Every target converter is paired; this heuristic is not behavioral or coverage proof.

Twenty-seven hand-authored, single-cause productive mutations compiled with zero warnings/errors and were killed by unchanged relevant tests. They covered both nested waits; array, list, dictionary-value and dictionary-key core waits; ten shape-adapter detachments; combined-entry caller detachment; missing sibling drain; synchronous/null value-invocation capture; and the five stop-before-next cancellation checks. The first attempted mutation exposed an unrelated compiler-only helper issue, was not counted, and was rerun after the helper correction. Every counted mutation was immediately restored. The final target search contains no `WaitAsync(cancellationToken)` and the post-mutation focused candidate passed 76/76.

## Requirement-to-evidence map

| Requirement | Final native evidence | Mutation witness |
| --- | --- | --- |
| Typed/runtime nested initialization owns accepted original outcomes | `NestedConverters_OwnAcceptedInitializerAndOriginalOutcomeAsync` — 6 cases | Separate typed and runtime detachments each fail 3 cases. |
| Injected nested dependencies and resolver/task results are non-null | `NestedConverters_RejectMissingInjectedDependenciesAndResolverResultsAsync` — 1 case | Exact constructor, resolver and task diagnostics asserted. |
| All single-conversion shapes own accepted tasks and enumerators | `SingleConversionCollections_OwnAcceptedTaskAndEnumeratorAsync` — 42 cases | Array, list, value-dictionary, key-dictionary and all adapters killed independently. |
| Every combined dictionary shape owns its value outcome | `CombinedDictionaryShapes_OwnAcceptedValueOutcomeAsync` — 12 cases | Combined core and three adapters killed independently. |
| Accepted key/value siblings are both drained with key-first priority | `CombinedDictionary_OwnsBothAcceptedTasksWithKeyFirstPriorityAsync` — 4 cases | Removing sibling drain fails all 4 cases. |
| Value invocation failure cannot abandon an accepted key | `CombinedDictionary_ValueInvocationFailureStillOwnsAcceptedKeyAsync` — 6 cases | Removing async invocation capture fails all 6 cases. |
| Cancellation stops before another conversion starts | `CollectionConverters_StopBeforeStartingAnotherConversionAsync` — 5 cases | Removing each family check fails its corresponding case. |

## Terminal validation

Repository-prescribed warning-as-error, no-restore Release builds passed with `--disable-build-servers -m:1 /nodeReuse:false /p:UseSharedCompilation=false`: ViciOne.ServiceBus.slnx in 1:17.69 and ViciOne.ServiceBus.Tests.Unit.slnx in 1:13.29, both with zero warnings/errors. The repository rule against `--no-incremental` on solution builds was followed. Both `dotnet format --verify-no-changes` gates for ViciOne.ServiceBus.Engineering.slnx and ViciOne.ServiceBus.Tests.Unit.slnx exited 0 without findings.

The unfiltered native Core profile passed 4,772/4,772 cases in 32.414 seconds with no failed, pending, skipped or other results. Exact bag reconciliation retained all 4,696 Iteration-139 names/multiplicities and added only the intended 76 cases with method multiplicities 6, 1, 42, 12, 4, 6 and 5. Canonical sorted-name/newline SHA-256 is `a1d092efff6afa3c476d85bbf4aed3469e2658347fc6e2267d812d59c1c8ffc9`; CTRF SHA-256 is `b73520ce83112ccd2559cbd862931027b84a39a31cad1c80c4a63c7b5449d5fb`.

The current selected coverage configuration SHA-256 is `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`. A separate 4,772/4,772 run collected 49,676/61,225 lines (81.1368%) and 17,147/23,326 branches (73.5102%) across the selected nine-assembly product graph. Cobertura SHA-256 is `69d22f8b5144dc132c39bc9787c2f62632c5616d6615fd7f3951c7e912985dcc`; coverage CTRF SHA-256 is `9d637f756d15ac297e1e89d59b953a6c809053a7254409a5b59c215a64072960`.

The coverage-analysis formula evaluated 18,148 methods, found 121 CRAP>30 hotspots and 4,479 below-threshold methods, 3,690 of them completely uncovered. The canonical complete below-threshold identity/newline SHA-256 is `a3c1bc4a44b45263d12d832f7421ee00f2e157e60d2e8e3e2c2a92fc1c69a7f2`. The highest measured hotspot remains RequestRateAlgorithm's constructor at complexity 34, 0% method-line coverage and CRAP 1,190. These figures are selected Core-graph evidence, not whole-fork coverage or a claim that the current converter packet caused those unrelated gaps.

Raw artifacts remain under `/private/tmp/vsb-iteration140-final-core`, `/private/tmp/vsb-iteration140-coverage`, the isolated pairing directory and mutation directories. Protected review, TestResults and legacy trees were not read or modified.

## Remaining original-goal gates

This packet does not prove whole-fork personal source/comment completion, global API/new-parameter test completeness, global bidirectional async naming and type/file/directory consistency, legacy/dummy/directive absence, whole-fork line/branch coverage and CRAP, or real durable-provider/external acceptance. Task-valued and remaining scalar/fallback converter chains are the next connected work. Normal commit, annotated tag, atomic private push and independent branch/tag-object/peeled-tag verification remain the final checkpoint operations for this iteration.
