# Iteration 140 — nested and collection converter ownership remediation

## Outcome and scope

This connected checkpoint corrects the nested-message, array, list, dictionary-value, dictionary-key and combined key/value converter chains. It remains a bounded part of the original whole-fork A+ source/API/architecture objective, not a completion claim.

Parent checkpoint: `c6d212d19d4bdd98b432f3f27f434dbbb69bd8c4`, normally committed, annotated-tagged, atomically pushed and independently verified. Its unfiltered Core multiset contains 4,696 passing cases. Iteration 140 adds 76 native cases without deleting or renaming a parent test or requirement binding.

Each converter still rejects a null context, honors pre-start cancellation, and forwards the exact token to nested or element conversion. Once a collaborator task is accepted, the converter now observes the producer-owned original to success, ordinary fault or producer cancellation instead of using caller-local `WaitAsync` detachment. Collection shape adapters likewise observe their accepted core traversal. Enumerators remain owned until current accepted work settles, while cancellation can still prevent the next conversion from starting.

The combined key/value converter starts both conversions for each accepted entry. Both tasks are now observed. A key fault or cancellation retains deterministic priority after the accepted value sibling settles. If value invocation throws synchronously or yields a null task after key acceptance, that invocation is represented as the value task outcome, the key still settles, and key-first priority remains intact.

No public signature, package, SDK pin, target framework or supported feature changes. Internal nested-converter constructor/resolver seams make accepted initializer tasks controllable in tests; parameterless production constructors retain MessageInitializerCache behavior.

## Read admission and research

The main agent fully read every file in `.testagent/iteration140/source-admission.tsv`: 10 files / 2,603 current lines, manifest SHA-256 `ecc12b5679261eac050858b285960e6896bb4b96c2796c25da392c5180072866`. This includes all five changed converter source files, their complete provider factory and initializer/cache/interface support. The owning test/support admission contains 5 files / 1,737 current lines, manifest SHA-256 `ab4778bd9f46521d6034c42139e9b27afe3f26afc5d01bf605cf581523bfe3bf`.

Chaining those manifests to the Iteration-139 source/test chain hashes yields `fbdcae2052a52091b2881d603e2fbad64df69d8a7d08163cdd528c1c97ee7247` and `92ae6d91e60ea9f076f1810173530e73753c880dd3c8fafc85ee27655867489f`. The chain records exact deltas without fabricating de-duplicated cumulative file or line totals.

The mandatory static pairing workflow ran once in an isolated Initializers-only copy under `/private/tmp`. It classified 87 source files and 49 tests: 75 paired and 12 unpaired. Every target converter was paired, while InitializePropertyConverter had only factory-selection coverage before this packet. Pairing is a navigation heuristic, not behavioral or coverage proof.

## Test-first baseline and final assertions

PropertyConverterOwnershipTests is handwritten and contributes seven methods / 76 cases:

| Method | Cases | Evidence |
| --- | ---: | --- |
| NestedConverters_OwnAcceptedInitializerAndOriginalOutcomeAsync | 6 | Typed/runtime forms × success, exact ordinary fault and producer cancellation; exact child context/input/token and pending ownership. |
| NestedConverters_RejectMissingInjectedDependenciesAndResolverResultsAsync | 1 | Constructor, runtime resolver and initializer-task null guards with exact diagnostics. |
| SingleConversionCollections_OwnAcceptedTaskAndEnumeratorAsync | 42 | Fourteen converting shapes × three outcomes; exact values, token, exception/cancellation, root state and disposal timing. |
| CombinedDictionaryShapes_OwnAcceptedValueOutcomeAsync | 12 | Four combined shapes × three value outcomes while key succeeds. |
| CombinedDictionary_OwnsBothAcceptedTasksWithKeyFirstPriorityAsync | 4 | Both accepted siblings settle; key fault/cancellation retains original priority. |
| CombinedDictionary_ValueInvocationFailureStillOwnsAcceptedKeyAsync | 6 | Synchronous/null value invocation × three key outcomes; no accepted key abandonment. |
| CollectionConverters_StopBeforeStartingAnotherConversionAsync | 5 | Array, list, value dictionary, key dictionary and combined dictionary stop before a second conversion. |

After only the nested injection seam was compiled, the unchanged waits produced 1 passed / 71 failed across the 72 then-existing cases. That is the causal behavior baseline. An earlier test compiler failure is not counted. The first corrected candidate passed 72/72. Complete assertion and gap review then added exact context identity, nested null-task coverage and the value-invocation/key-priority matrix, producing the final 76/76 focused result. Existing collection, converter, object-graph and factory contracts separately passed 25/25 before the complete host run.

Assertions cover exact reference/value/structure, context/input/token forwarding, exact exception identity and cancellation token, pending and terminal task states, negative second-call behavior, and enumerator disposal count/timing. Cleanup independently settles and observes held originals. No assertion-free, trivial-only, tautological or fire-and-forget exception method remains. Every Task-returning test has the Async suffix.

CoreRequirements.json retains the exact 3,009-entry parent prefix and appends seven unique entries. The complete Core host's compiled metadata projection passed, providing bidirectional method/binding evidence rather than a JSON-only check.

## Compiled single-cause mutations

Twenty-seven counted mutations compiled cleanly and were killed by unchanged relevant tests:

| Mutation group | Count | Targeted failures |
| --- | ---: | ---: |
| Typed and runtime nested WaitAsync detachment | 2 | 3 each |
| Array accepted-element detachment | 1 | 4 |
| List core plus four shape-adapter detachments | 5 | 16; then 3 each |
| Dictionary-value core plus three adapters | 4 | 13; then 3 each |
| Dictionary-key core plus three adapters | 4 | 13; then 3 each |
| Combined core plus three adapters | 4 | 17; then 3 each |
| Remove accepted value-sibling drain | 1 | 4 |
| Remove async capture of value invocation outcome | 1 | 6 |
| Remove stop-before-next check for five converter families | 5 | 1 each |

The first attempted mutation revealed a compiler-only issue in a test helper, was excluded, and was rerun after correction. Every counted mutation was restored immediately. Final hashes of the five product files and ownership fixture match the admission manifests, no target `WaitAsync(cancellationToken)` remains, and the restored post-mutation fixture passed 76/76.

## Requirement-to-evidence map

| Requirement | Native evidence | Causal evidence |
| --- | --- | --- |
| Nested accepted-task ownership | 6 outcome/form cases | 6 original failures; both reinstated waits killed. |
| Nested dependency/result guards | 1 guard case | The only baseline pass; exact diagnostics retained. |
| Single-conversion shape and enumerator ownership | 42 cases | All core and adapter detachments killed. |
| Combined shape value ownership | 12 cases | Core/adapters killed independently. |
| Both siblings settle with key priority | 4 cases | Missing drain killed. |
| Value invocation cannot abandon accepted key | 6 cases | Removed async capture killed. |
| Cancellation prevents another conversion | 5 cases | Each removed family check killed. |

## Terminal validation and selected coverage

| Gate | Result | Duration |
| --- | --- | ---: |
| ViciOne.ServiceBus.slnx Release build, no restore, warnings as errors | 0 warnings / 0 errors | 1:17.69 |
| ViciOne.ServiceBus.Tests.Unit.slnx Release build, no restore, warnings as errors | 0 warnings / 0 errors | 1:13.29 |
| Engineering formatting verification | Exit 0, no findings | — |
| Unit formatting verification | Exit 0, no findings | — |
| Unfiltered native Core | 4,772 passed; 0 failed/skipped/other | 32.414 s |

Builds used disabled build servers, one MSBuild node, no node reuse and no shared compiler to avoid local server contention. They followed `docs/build.md`: solution builds did not combine `--no-incremental` with the strict gate.

Exact multiset reconciliation retained every one of the 4,696 parent names/multiplicities and added exactly 76 intended cases with method multiplicities 6, 1, 42, 12, 4, 6 and 5. Canonical sorted-name/newline SHA-256 is `a1d092efff6afa3c476d85bbf4aed3469e2658347fc6e2267d812d59c1c8ffc9`; final Core CTRF SHA-256 is `b73520ce83112ccd2559cbd862931027b84a39a31cad1c80c4a63c7b5449d5fb`.

The current coverage configuration SHA-256 is `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`. A separate complete native run passed 4,772/4,772 and measured the selected nine-assembly product graph at 49,676/61,225 lines (81.1368%) and 17,147/23,326 branches (73.5102%). Cobertura SHA-256 is `69d22f8b5144dc132c39bc9787c2f62632c5616d6615fd7f3951c7e912985dcc`; coverage CTRF SHA-256 is `9d637f756d15ac297e1e89d59b953a6c809053a7254409a5b59c215a64072960`.

The documented CRAP formula evaluated 18,148 methods: 121 exceed CRAP 30; 4,479 are below the 80% line / 70% branch method thresholds, including 3,690 at 0% measured method-line coverage. Their complete canonical identity/newline SHA-256 is `a3c1bc4a44b45263d12d832f7421ee00f2e157e60d2e8e3e2c2a92fc1c69a7f2`. RequestRateAlgorithm's constructor is the largest selected hotspot (complexity 34, 0% coverage, CRAP 1,190). This is selected Core-graph coverage, not whole-fork coverage, and unrelated hotspots are not attributed to this converter packet.

Raw evidence remains under `/private/tmp/vsb-iteration140-final-core`, `/private/tmp/vsb-iteration140-coverage`, the isolated pairing directory and mutation directories. Nothing is staged into protected review, TestResults or legacy trees.

## Checkpoint and original-goal continuation

The checkpoint includes five corrected product files, the new ownership fixture, the requirement-ledger append, five iteration records and this evidence report. The intended annotated tag is `servicebus-a-plus-iteration-140-nested-and-collection-converter-ownership-remediation-2026-09-16`. Normal commit, atomic private push and independent branch/tag-object/peeled-tag verification occur after final non-protected diff review; this report does not pre-claim them.

Whole-fork personal source/comment reading, global API/new-parameter behavior coverage, global naming/type/file/directory audits, legacy/dummy/directive absence, whole-fork coverage/CRAP and real durable-provider/external acceptance remain open. The next connected packet is task-valued and remaining scalar/fallback converter ownership; successful collection gates do not certify those paths.
