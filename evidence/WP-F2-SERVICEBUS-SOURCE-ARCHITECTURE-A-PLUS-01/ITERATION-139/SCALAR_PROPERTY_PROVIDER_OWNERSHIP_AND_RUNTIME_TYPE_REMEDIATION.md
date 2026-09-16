# Iteration 139 — scalar property-provider ownership and runtime-type remediation

## Outcome and scope

This connected iteration corrects PropertyConverterPropertyProvider, VariablePropertyProvider, ObjectPropertyProvider, FromNullablePropertyProvider and ToNullablePropertyProvider, plus the DictionaryInitializerConvention boundary that selects runtime object conversion. It is a bounded checkpoint within the original whole-fork A+ source/API/architecture objective, not a whole-goal completion claim.

Parent checkpoint: `5c521abbfb72709dbc3709d8d8bde738691207e4`, normally committed, annotated-tagged, atomically pushed and independently verified. Its Core multiset contains 4,645 passing cases. Iteration 139 adds 51 native cases without deleting an existing test method or requirement binding.

Caller cancellation is forwarded to source providers, converters and initializer variables. Once one of those operations has been accepted, the composing provider observes its producer-owned original task to success, ordinary fault or provider cancellation rather than using WaitAsync to detach locally. AsyncPropertyProvider's caller-owned task-valued input remains locally cancellable and is deliberately unchanged.

ObjectPropertyProvider now preserves an assignable runtime reference instance before converter lookup. This repairs IDictionary<string, object> initialization for arrays, custom collection classes and ordinary reference objects without broadening PropertyProviderFactory.Matching into a new global converter contract. The dictionary object fallback is restricted to reference-type targets, so unsupported structs fall through to a clean false/null result instead of failing MakeGenericType. The runtime converter cache is a ConditionalWeakTable keyed by runtime Type, so collectible negative runtime types and associated converter holders are not retained for the provider lifetime. Sequential converter reuse remains covered; no exactly-once construction promise under contention is invented.

No public signature, package, SDK pin, target framework or supported feature changes. The nested runtime converter abstraction is renamed IObjectConverter, provider comments distinguish cooperative token forwarding from accepted-task ownership, and no work-process narrative or suppression directive enters product source.

## Read admission and research

The main agent fully read every file in `.testagent/iteration139/source-admission.tsv`: 12 files / 1,503 current lines, manifest SHA-256 `79d28a307aef90f467115dd487d60eb076d106a252c67b1599a42dc6bf1e7e12`. This includes the five target providers, complete 836-line PropertyProviderFactory and all its Matching consumers, both convention owners and the four exact interfaces. The owning test/support admission contains 13 files / 2,684 current lines, manifest SHA-256 `243e7a60185c8dc4828acaf1bdd51ff56612262ea98315cbb087584fff13bc6a`.

The prior cumulative metadata hashes are chained with these exact delta manifests as source `8636020538e7c36fcb93129e1e131c1052ff34b51a6156161a264247baf346c7` and tests `6c2149b9be467cb44760681c42666f7d853666f5e0512e346d7fdc901395472f`. No de-duplicated cumulative count is fabricated because Iteration 138 did not retain the canonical full path manifests needed to calculate one honestly.

The required static source-to-test pairing ran once against an isolated Initializers-only copy outside the repository's protected trees. It classified 88 sources and 48 tests: 76 paired and 12 unpaired. Every target provider, PropertyProviderFactory, DictionaryInitializerConvention and DefaultInitializerConvention was paired. Static pairing is a navigation heuristic, not an assertion-strength, behavior or coverage verdict.

## Test-first baseline and final assertions

PropertyProviderOwnershipTests is handwritten and contributes seven methods / 51 cases:

| Method | Cases | Evidence |
| --- | ---: | --- |
| PropertyConverterProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync | 12 | Two accepted stages × three producer outcomes × caller active/canceled; exact context/input/token, pending ownership and original result. |
| VariableProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync | 12 | Provider and variable stages with the same complete outcome/caller matrix. |
| ObjectProvider_OwnsEachAcceptedStageAndOriginalOutcomeAsync | 12 | Object source and selected runtime conversion with exact collaborator effects. |
| NullableProviders_OwnAcceptedProviderAndOriginalOutcomeAsync | 12 | Both nullable adapters × three outcomes × caller active/canceled. |
| DictionaryObjectValues_PreserveAssignableRuntimeInstancesAsync | 1 | Exact array, custom collection and payload identities through full dictionary initialization. |
| DictionaryObjectFallback_RejectsUnsupportedStructWithoutActivationFailure | 1 | False/null clean rejection, with no reflective generic-constraint exception. |
| ObjectProvider_DoesNotRetainCollectibleRuntimeTypes | 1 | Collectible runtime type and assembly release while the provider remains alive. |

The existing ComposedProviders_ObserveCallerCancellationWhileDependenciesRemainPendingAsync method and binding remain. It continues to prove caller-local cancellation for AsyncPropertyProvider while now requiring the corrected converter/variable providers to remain pending until their accepted originals settle.

The unchanged productive source compiled before the focused baseline. The exact 53 cases reported 25 passed and 28 failed: 24 caller-cancellation lifetime failures across eight accepted waits, one existing composed-provider failure, and one failure each for runtime identity, clean struct rejection and collectibility. The corrected candidate passed the exact 53/53, with no pending, skipped or other results. CoreRequirements.json retains the exact 3,002-entry parent prefix and appends seven unique bindings, giving 3,009 entries.

Final manual assertion review reopened both complete fixtures. Their source contains 63 direct Assert calls across nine public test methods, excluding theory multiplicity. Assertions cover exact values, reference identities, context/input/token forwarding, exception identity, producer cancellation token, pending/success/fault/canceled state, null/false results, negative collaborator effects and weak-reference release. Cleanup independently settles and observes originals and roots; watchdog timeouts cannot be swallowed. No assertion-free, trivial-only, tautological or fire-and-forget exception test remains. Bidirectional Async naming is correct for all changed methods.

## Compiled single-cause mutations

Eleven hand-authored mutations compiled with zero warnings/errors and were killed by unchanged relevant tests:

| Mutation | Executed | Passed | Failed | Intended witness |
| --- | ---: | ---: | ---: | --- |
| PropertyConverter source WaitAsync | 12 | 9 | 3 | Canceled caller detached from pending accepted source. |
| PropertyConverter conversion WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted conversion. |
| Variable provider WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted provider. |
| Variable value WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted variable. |
| Object source WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted object source. |
| Object conversion WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted runtime conversion. |
| FromNullable WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted nullable source. |
| ToNullable WaitAsync | 12 | 9 | 3 | Canceled caller detached from accepted value source. |
| Remove assignable-value identity bypass | 1 | 0 | 1 | Exact runtime instance was lost. |
| Remove dictionary reference-type guard | 1 | 0 | 1 | Unsupported struct threw reflective constraint exception. |
| Restore strong ConcurrentDictionary cache | 1 | 0 | 1 | Collectible runtime type remained alive. |

All 99 executions were discovered, with 72 passed / 27 targeted failures and no pending, skipped or other results. Every mutation was restored before the next attempt. The final source search contains no provider WaitAsync, and terminal compilation used ConditionalWeakTable, the identity bypass and the reference-type guard.

## Terminal validation and coverage

Warning-as-error, no-restore, non-incremental Release builds passed:

| Gate | Result | Duration | Artifact SHA-256 |
| --- | --- | ---: | --- |
| ViciOne.ServiceBus.slnx | 0 warnings / 0 errors | 56.27 s | `a59103a88bb1f1874a3eedb83385b8eab4992f7e22717f3bd4d8f5cb0dc8fa09` |
| ViciOne.ServiceBus.Tests.Unit.slnx | 0 warnings / 0 errors | 1:56.77 | `0aa2747230145693e3a16087ef0d80d5cef36a5f20ead05f63b7638b34b5f6fb` |

The unfiltered native Core run passed 4,696/4,696 in 26.364 seconds, with no failed, pending, skipped or other outcomes. Exact multiset reconciliation retained all 4,645 parent names/multiplicities and added only the intended 51 cases with multiplicities 12, 12, 12, 12, 1, 1 and 1. Canonical sorted-name/newline SHA-256 is `c10ee9164325df7819015b0c87ce08e66c4158d830773aa01c6ba1c26790488d`; CTRF SHA-256 is `9d06160cd36d09f2b616a545773233c8d7dfb0261a5a053bdd03e043d8bc926b`.

The selected productive coverage configuration remains SHA-256 `c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13`. A separate native run passed the same 4,696 cases and produced:

| Product assembly | Lines covered / valid | Branches covered / valid |
| --- | ---: | ---: |
| ViciOne.ServiceBus.Abstractions | 4,940 / 8,310 | 1,612 / 2,996 |
| ViciOne.ServiceBus.Sagas | 4,825 / 7,512 | 1,487 / 2,571 |
| ViciOne.ServiceBus.JobService | 4,353 / 4,552 | 2,067 / 2,303 |
| ViciOne.ServiceBus.Courier | 2,526 / 2,835 | 682 / 904 |
| ViciOne.ServiceBus | 28,550 / 35,520 | 10,124 / 13,633 |
| ViciOne.ServiceBus.Futures | 1,356 / 1,495 | 424 / 496 |
| ViciOne.ServiceBus.Testing | 3,176 / 3,365 | 882 / 1,131 |
| ViciOne.ServiceBus.Initializers | 100 / 100 | 4 / 4 |
| ViciOne.ServiceBus.Mediator | 836 / 918 | 247 / 314 |
| Selected total | 50,662 / 64,607 (78.4157%) | 17,529 / 24,352 (71.9818%) |

Package counts sum exactly to the Cobertura root. Cobertura SHA-256 is `c52d2f8c7a0a661adfe11e5c7c9f844199003e0eba8595ec25db32a872e1fed5`; coverage CTRF SHA-256 is `fe66a348659e7ae8ecbf18c4fbc3e79f14c0512417edc2e2822d8c8964d8aa8b`. PowerShell was unavailable, so the coverage-analysis skill's documented inline fallback applied the same CRAP formula and 80/70 thresholds: 18,663 methods, 150 methods with CRAP>30, 4,877 below-threshold members and 4,073 completely uncovered members. The canonical complete below-threshold member-name/newline hash is `aa28c6f85f2dfd752c9f3e058f70d075642a68758a23554e539010da4bc253a8`.

Generated System.Text.Json context methods occupy nine of the ten highest CRAP positions. The highest handwritten hotspot is RequestRateAlgorithm's constructor (complexity 34, 0% measured method-line coverage, CRAP 1190). These figures are selected-Core evidence, not whole-fork coverage, and the CRAP inventory is not presented as an Iteration-139 provider defect list.

Raw artifacts remain under `/private/tmp/vsb-iteration139-final-core`, `/private/tmp/vsb-iteration139-coverage` and `/private/tmp/vsb-i139-m01` through `m11`. They are local evidence and are not staged into protected result trees.

## Checkpoint and original-goal continuation

The checkpoint is designed to include the six corrected product files, two changed existing test/ledger files, the new ownership fixture, three iteration test-agent records, two admission manifests and this evidence report. The annotated tag is `servicebus-a-plus-iteration-139-scalar-property-provider-ownership-and-runtime-type-remediation-2026-09-16`. Normal atomic private push and independent branch/tag-object/peeled-tag verification occur only after final diff review; this report does not pre-claim them.

Whole-fork personal source/comment reading, global API/new-parameter behavior coverage, global naming/type/file/directory audits, legacy/dummy/directive absence, whole-fork line/branch coverage and CRAP, and real durable-provider/external acceptance remain open original-goal gates. The next connected work is the nested-message, fallback and collection/key converter chain; successful scalar gates do not certify those paths.
