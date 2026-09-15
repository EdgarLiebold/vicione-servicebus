# Iteration 134 — Consume notification contract remediation

Date: 2026-09-16.
Branch: `feature/servicebus-a-plus-api`.
Parent: `a0942e669107371858c942526b85c797b3021e15`.
Development slice: `WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`.
Slice SHA256: `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.

## Outcome and limits

The shared consume-notification entrypoints validate required arguments synchronously before caller cancellation. A pre-cancelled caller receives a task cancelled with the original token, without invoking fault generation or the receive pipeline. Successful notification preserves the actual receive task, including its identity and completion. Null generation and receive tasks are explicit operation-specific contract errors, rather than incidental null-reference failures or a null returned to the caller.

Fault notification retains its two existing phases. Matching cancellation and an already-cancelled delivery suppress fault generation according to the **passed message context**, not the owner context. Otherwise the actual generation task is awaited to completion. Later caller or delivery cancellation cannot detach that task, replace its original failure/token, or start receive notification prematurely. After successful generation the original caller token is forwarded to receive notification. The real receive implementation can then cancel before recording fault state or invoking observers. Consequently a fault can have been generated while the subsequent receive notification is cancelled; these phases are deliberately not presented as an atomic operation.

No feature or public signature was removed. No linked cancellation source, cancellation-only wait, second task-registration mechanism, legacy alias, warning suppression, conditional-compilation workaround or authored-code generator was introduced. Mediator's intentional suppression of transport fault publication remains unchanged.

The fresh corrected owning compiler completed with zero warnings/errors and exit 0. Its focused native run passed all 34 cases, with zero failed, skipped, pending or other and native exit 0. After all counterchanges and exact restoration, the final compiler again exited 0 with zero warnings/errors. The fresh unfiltered Core run passed all 4093 cases, including the exact multiset of all 4059 previous cases and precisely the 34 new cases. All eight distinct compiled counterchanges were killed at their intended oracles; the delivery-detachment counterchange was additionally recompiled/repeated after cleanup improvement. The original whole-source A+ API/architecture goal remains active; this bounded result is not proof of universal correctness, complete API/parameter coverage or A+ status for the entire fork.

## Authority and owning-test admission

The existing full personal readings of the applicable root governance, repository rules and hash-bound development slice were retained only after checking that their current hashes are unchanged. No unrelated Suite product order or internal reviewer was used as an external product-team role.

Before test edits, read-only revalidation accepted all 623 exact owning inputs at the parent commit: Core 559, shared support/build graph 64. Each working file's Git blob hash matched the parent tree; the complete current Core Git inventory was contained in the retained personal full-read set. This revalidation confirms unchanged bytes, not a substitute claim that hashing constitutes source reading. New test code was then personally read through EOF. The complete requirement ledger parses, all 2933 parent rows remain an identical semantic prefix, and exactly nine unique requirement/variant bindings were manually appended; the ledger now has 2942 unique keys.

The final owning read set has 624 inputs: Core 560 plus the unchanged 64 support/build inputs. Its sorted path/content SHA256 is `bf8f6867cfad73279c7e881b3c6ba5c3dddf8023bbcbe383bee50adc4d1bdcc4`. The two changed Core inputs are the personally read new 617-line test and its manually appended ledger rows. The final unfiltered run additionally executes the current embedded ledger's owning checks.

Excluded review, TestResults and legacy trees were not opened or modified. Shared test foundations, project/compiler settings, package graph and workflow verdicts were not changed.

## Requirement-to-test evidence

All methods belong to `ViciOne.ServiceBus.Tests.Context.Consumption.ConsumeNotificationContractTests`, in assembly `ViciOne.ServiceBus.Tests`. Every row has a clean focused native result from the freshly compiled corrected source, not from a failed build or an older DLL. All exact case counts below were independently reverified in the final 4093/4093 unfiltered run against the final cleanup-enhanced test file.

| Requirement variant | Exact test method | Cases | Clean focused evidence |
| --- | --- | ---: | --- |
| synchronous-arguments-before-cancellation | `InvalidArguments_AreRejectedSynchronouslyBeforeCancellation` | 4 | 4 passed |
| pre-cancellation-has-no-effects-and-keeps-caller-token | `PreCanceledNotification_HasNoEffectsAndPreservesTheCallerTokenAsync` | 4 | 4 passed |
| held-generation-is-owned-before-receive-and-caller-cancellation | `HeldGeneration_IsOwnedAndCallerCancellationDoesNotDetachItAsync` | 2 | 2 passed |
| held-generation-failure-wins-over-later-caller-cancellation | `HeldGenerationFailure_PreservesItsOriginalOutcomeAndNeverNotifiesAsync` | 4 | 4 passed |
| null-generation-or-receive-task-has-specific-diagnostic | `NullGenerationOrReceiveTask_HasAnOperationSpecificDiagnosticAsync` | 3 | 3 passed |
| generation-failure-remains-original-without-receive | `GenerationFailure_PreservesTheOriginalExceptionAndNeverNotifiesAsync` | 2 | 2 passed |
| fault-suppression-uses-passed-message-context-token | `FaultSuppression_UsesThePassedMessageContextTokenAsync` | 7 | 7 passed |
| held-receive-completion-stays-owned-after-caller-cancellation | `HeldReceiveCompletion_RemainsOwnedAfterCallerCancellationAsync` | 2 | 2 passed |
| receive-failure-or-cancellation-preserves-original-outcome | `ReceiveCompletion_PreservesItsOriginalOutcomeAsync` | 6 | 6 passed |

Argument oracles check exact exception types/parameter names, multiple-invalid-input order, null/empty/whitespace consumer identities, active and cancelled callers, no generation and no receive/state/payload/observer effect. Pre-cancellation is exercised both against the real base receive implementation and against a strict direct receive boundary, preventing accidental dependence on the downstream implementation to enforce the consume contract.

Held-generation tests use actual pending tasks and separately check root completion, generation arguments, no receive entry and no observer/state effects before release. After successful generation, a live caller reaches a held observer; a cancelled caller reaches the receive boundary with its exact token but causes no fault-state/payload/observer effect. Generation failure and cancellation retain their original exception instance or independent generation token despite later caller cancellation. Two cases additionally cancel the passed delivery while generation remains held.

All held tasks are released in `finally`. Cleanup boundedly observes settled outcomes with a non-cancelled cleanup token; genuinely incomplete work still times out. The held-generation failure cleanup also observes the actual generation task explicitly, independently of whether the product correctly owns it. The intended outcome assertions are separate from cleanup. No sleep, timer-duration guess or blocking task result is used as an asynchronous oracle. Observer-entry snapshots verify delivered/faulted state before callbacks, separately from the final state.

The fixture uses a real base receive context, actual mediator message/serialization contexts, the canonical metadata codec and real payload storage. Only the protected virtual generation seam is overridden to control its task; strict setup/boundary proxies throw on unexpected operations. These tests therefore do not execute the default generation endpoint route, broker/database/cloud acceptance or durable-sender provider acceptance. The existing response-lifetime/fault-routing tests remain separate evidence and are not removed as if fully replaced by this cohort.

## Structured diagnosis and internal counterreview

The unchanged source was compiled successfully before its baseline run. The baseline executed all 32 then-declared cases: 23 passed and nine failed at intended argument, caller-pre-cancellation or null-task oracles; native exit 2. Existing faults were not inferred merely from tests being written. No assertion was relaxed to obtain a passing result.

One initial native invocation used an unsupported CTRF spelling and exited 5 before executing any test. The actual compiled runner help identifies `--report-xunit-ctrf` and `--report-xunit-ctrf-filename`. Correcting only those options produced the genuine baseline above; no rebuild, restore, clean, skipped test or relaxed gate was used. Native report syntax is now taken from that current runner. Builds use the previously diagnosed unsandboxed SDK IPC path, explicit owned temporary output and unique binary logs rather than repeating failed sandbox invocations.

An explicitly authorized internal Sol reviewer independently read the bounded source and all new tests/ledger bindings. It found a Medium oracle gap: only caller cancellation, not delivery cancellation, was applied while generation was held. The two additional cases close that gap through the original exception/token outcome. The amended static review binds the exact four candidate file hashes and reports no remaining concrete finding in this scope. It is not external independent acceptance or a substitute for the lead's actual build/native/mutation evidence.

A further narrow counterreview confirmed that explicit observation of the generation task improves cleanup hygiene for deliberately detached product implementations. Exactly one line was added inside that test's `finally`, after the first seven counterchanges. A full before/after byte comparison proves there was no other test edit: all assertions, attributes, case rows and earlier M01–M06 selected method bodies remain unchanged. Those executions use test SHA256 `b91324fd3d6ccdea967523fb7cc6b9db44298e4650fd64a29e76d59d35682344`; the final test file is `e10facc8b9f337852a35b5a4170cd3ad7eace0f65c47a85dcc48c7d6b7a8124e`. M07 is additionally repeated after this cleanup edit; the final fresh compiler and unfiltered run must use the final test bytes. No unobserved exception leak is declared measured from the hygiene improvement.

## Causal compiled counterchanges

Each counterchange is a handwritten temporary edit to `BaseConsumeContext.cs`, without changing tests. Native execution is permitted only after its owning compiler actually exits 0. After every result, manual restoration must independently match candidate source SHA256 `137cf601244d214fbeaba92596740b569f933644f2c0ee0719e21ffaf084f72a` before the next edit.

| Counterchange | Exact selected method | Executed / passed / failed | Actual outcome |
| --- | --- | --- | --- |
| M01: successful-notification cancellation bypasses required guards | `InvalidArguments_AreRejectedSynchronouslyBeforeCancellation` | 4 / 3 / 1 | Compiler 0; native 2; intended pre-cancelled successful case no longer throws synchronously; restored SHA exact |
| M02: caller pre-cancellation no longer suppresses fault generation | `PreCanceledNotification_HasNoEffectsAndPreservesTheCallerTokenAsync` | 4 / 2 / 2 | Compiler 0; native 2; both intended faulted cases reject generation or a non-cancelled boundary task; restored SHA exact |
| M03: successful-notification null receive task is returned unchecked | `NullGenerationOrReceiveTask_HasAnOperationSpecificDiagnosticAsync` | 3 / 2 / 1 | Compiler 0; native 2; intended success boundary no longer throws; restored SHA exact |
| M04: null generation task is awaited unchecked | `NullGenerationOrReceiveTask_HasAnOperationSpecificDiagnosticAsync` | 3 / 2 / 1 | Compiler 0; native 2; intended generation boundary rejects `NullReferenceException` instead of its explicit diagnostic; restored SHA exact |
| M05: null consume-fault receive task is awaited unchecked | `NullGenerationOrReceiveTask_HasAnOperationSpecificDiagnosticAsync` | 3 / 2 / 1 | Compiler 0; native 2; intended fault notification boundary rejects `NullReferenceException` instead of its explicit diagnostic; restored SHA exact |
| M06: matching cancellation is compared with the owner token instead of the passed message token | `FaultSuppression_UsesThePassedMessageContextTokenAsync` | 7 / 4 / 3 | Compiler 0; native 2; three independent passed-context branch oracles reject wrong suppression/generation; restored SHA exact |
| M07: generation is detached with `WaitAsync(context.CancellationToken)` | `HeldGenerationFailure_PreservesItsOriginalOutcomeAndNeverNotifiesAsync` | 4 / 2 / 2 | Compiler 0; native 2; both delivery-cancelled cases reject completion while generation is still held; restored SHA exact |
| M08: actual receive completion is replaced with a completed task | `HeldReceiveCompletion_RemainsOwnedAfterCallerCancellationAsync` | 2 / 1 / 1 | Compiler 0; native 2; intended faulted case rejects root completion while its receive task remains held; final test bytes; restored SHA exact |

All eight distinct counterchanges are killed: 30 selected cases, 18 passed and 12 intentionally failed, with zero skipped, pending or other. M07 was recompiled and repeated against the final cleanup-enhanced tests: four cases, two passed/two intentionally failed, native 2, the same held-root oracles, and exact source restoration. Across the eight counterchanges plus this repeat, 34 cases were executed, 20 passed and 14 intentionally failed; all nine compilers exited 0. The subsequent exactly restored final compiler and fresh 4093/4093 unfiltered native run both exited 0. No global mutation score is claimed.

## Personal source reading, comments and structure

Eleven productive files, 2431 lines at the current candidate, were personally read in full. Earlier unchanged read ranges were retained; the final `PublishEndpoint.cs` range was explicitly read through EOF. Inventory/hash checks are read-only and do not author source, comments or report prose. The notification XML comments were written manually after understanding the corresponding implementation and the delivery/caller phase distinction.

The ordered path/line-count/content source-manifest SHA256 is `44ba239000268bb731063d4a41016004838446bb2d77547ac609c0562a3264f0`. All eleven current file hashes were independently checked after the final exact source restoration. This is a bounded read inventory, not a whole-fork completion claim.

| Productive source | Lines |
| --- | ---: |
| `src/ViciOne.ServiceBus/Context/Consumption/BaseConsumeContext.cs` | 419 |
| `src/ViciOne.ServiceBus.Abstractions/ConsumeContext.cs` | 197 |
| `src/ViciOne.ServiceBus/Context/Consumption/DeserializerConsumeContext.cs` | 75 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/ReceiveContext.cs` | 80 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorConsumeContext.cs` | 100 |
| `src/ViciOne.ServiceBus/Transports/Receiving/BaseReceiveContext.cs` | 275 |
| `src/ViciOne.ServiceBus/Advanced/ConsumeContextEndpointExtensions.cs` | 272 |
| `src/ViciOne.ServiceBus.Abstractions/Transports/PublishEndpoint.cs` | 239 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/SerializerContext.cs` | 70 |
| `src/ViciOne.ServiceBus/Context/Consumption/MessageConsumeContext.cs` | 386 |
| `src/ViciOne.ServiceBus/Advanced/LogContext.cs` | 318 |
| **Total** | **2431** |

The new test filename matches its primary type and existing owning namespace/folder. The private `NotifyFaultedCoreAsync` returns/awaits a task and carries the Async suffix; public notification methods still return asynchronous operations even though their validation wrapper is not an `async` state machine. That distinction is preserved rather than removing Async from a genuinely asynchronous contract. No directory restructuring decision was reopened or repeated as implementation work.

## Adjacent open work and continuation

The iteration-133 consume-layer pre-cancellation finding is implemented and focused-tested here. Partial receive-constructor ownership and base/Mediator cancellation logging parity remain separate open work. Full reading of `LogContext.cs` establishes asynchronous-flow-local logging, not a measured logging outcome. Default response/fault endpoint resolution still needs its own null task/result boundary evidence. Whole-source comments, all API/parameter tests, full-fork code/branch coverage and CRAP, global bidirectional Async naming, filenames/type/folder architecture, legacy/dummy/directive absence and real provider acceptance are not certified by this bounded package.

## Raw provenance and checkpoint

### Final restored execution and measured coverage

The final owning Release compiler completed in 11.38 seconds, with zero warnings/errors and actual exit 0. The subsequently executed owning native DLL ran without class/method filters: 4093 passed, zero failed/skipped/pending/other, actual native exit 0, 21.700 seconds. An independent complete CTRF parse checks every per-case status, all nine new method counts and the exact 4059-case parent multiset. The first read-only comparison used `Array.tally`, which is absent in the installed Ruby; the comparison was corrected to equivalent `group_by`/counts and exited 0. The authoritative successful native run was not restarted merely because that separate diagnostic parser failed.

Native CodeCoverage 18.10.0 collection occurred in that same unfiltered run. The collector settings parse and are byte-identical to iteration 133, SHA256 `c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13`. The complete Cobertura document parses; attribute-only native XML inspection independently confirms exactly the nine intended productive modules. Auto-properties are included, default attribute exclusions are disabled, test assemblies are excluded and only dynamic managed instrumentation is enabled. No source/comment/report generator or CRAP pipeline is credited with these measurements.

Weighted measured line coverage is **50630/64581 = 78.3977%**. Weighted measured branch coverage is **17476/24314 = 71.8763%**. These denominators cover the nine observed Core modules, not all productive persistence, scheduling or transport adapters. No whole-product coverage, 100% API/parameter testing or current whole-product CRAP result is claimed.

| Observed productive module | Line % | Branch % |
| --- | ---: | ---: |
| ViciOne.ServiceBus | 80.3375 | 74.0713 |
| ViciOne.ServiceBus.Abstractions | 59.4465 | 53.8051 |
| ViciOne.ServiceBus.Courier | 89.1005 | 75.4425 |
| ViciOne.ServiceBus.Futures | 90.7023 | 85.4839 |
| ViciOne.ServiceBus.Initializers | 100.0000 | 100.0000 |
| ViciOne.ServiceBus.JobService | 95.6283 | 89.7525 |
| ViciOne.ServiceBus.Mediator | 91.0675 | 78.6624 |
| ViciOne.ServiceBus.Sagas | 64.2705 | 57.8763 |
| ViciOne.ServiceBus.Testing | 94.3834 | 77.9841 |

### Final artifact hashes

| Input or result | SHA256 |
| --- | --- |
| Loaded owning native test DLL | `7392014e64b21c0b40aa8a4f23d99730ff1402b650ef6635a386cd130f208bb1` |
| Loaded owning productive ServiceBus DLL copy | `64a4aef55238598ed0093b53a82590260efbc19e920b0135a0300ea8410d4947` |
| Final Core CTRF | `55f1675cb2a8cd3db80f0da311647cc2cd38eec4bef52331ef6d22eadb9f8524` |
| Final Cobertura | `ccd5798a7822c9f6e734ced09f69ca8213d2a73f5576336ef8aef856172ebb20` |
| Final restored build log | `99781f34b12aab6ff447c99f6e61dab9dac2f2e97431d4166fa541dc87b9fef7` |
| Final native log | `94b7bdd6bcf43e73eada1c8e7792db595b0998f06b01c76e05c5f414443ff153` |

Raw logs, runner help, unique build binary logs and native CTRF records are isolated under `/private/tmp/vsb-iteration134-consume-notification.EbgYU9`. Baseline: `baseline-build.log` and `baseline-focused-v2/baseline-focused.ctrf.json`. Clean focused: `healthy-build.log` and `healthy-focused/healthy-focused.ctrf.json`. Final: `final-build.log`, `final-core-native.log`, `final-core/final-core.ctrf.json` and `final-core/final-core.cobertura.xml`. Each `M01`–`M08` and `M07R` has its own build/native log, unique binary log and CTRF. All native runs use explicit owned results, strict zero-test policy, exact minimum expected case counts, fail-skips/fail-warns, progress off and a bounded runner timeout. The final unfiltered run sets minimum 4093 and timeout five minutes. This is not a current whole-fork coverage result.

Candidate hashes: BaseConsumeContext `137cf601244d214fbeaba92596740b569f933644f2c0ee0719e21ffaf084f72a`; ConsumeContext `0293ff373678d391b8bf26d8bce9511147d7b12b21cbb7d6a3a222ea82ee6b0f`; final new tests `e10facc8b9f337852a35b5a4170cd3ad7eace0f65c47a85dcc48c7d6b7a8124e`; ledger `48bab09d8892bdeddbd5b589524981b53e2f68158f324e02c9a85dc702b123ab`.

The scoped checkpoint includes only the two productive source/comment files, the handwritten test, its nine ledger bindings and this report. The new annotated tag must name consume-notification remediation, not completion of the full A+ goal. Normal atomic push is limited to the approved feature branch and new tag, without force. Branch, tag object and peeled commit must be independently verified; an accepted push alone is not verification. Unrelated and excluded trees remain untouched.

The exact single origin push destination was rechecked as `git@github.com:EdgarLiebold/vicione-servicebus.git`. A fresh read-only GitHub API response confirms `EdgarLiebold/vicione-servicebus` is private with administrator/push permission. The existing branch workflow is unchanged; no new publication authority or external team role is inferred from backup approval.
