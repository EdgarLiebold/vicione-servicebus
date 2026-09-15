# Iteration 133 — Receive notification contract remediation

Date: 2026-09-16.
Branch: `feature/servicebus-a-plus-api`.
Parent: `d071332b777fbc13914cf5a779796e4d0b97f824`.
Development slice: `WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`.
Slice SHA256: `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.

## Outcome and limits

The three Mediator receive-notification operations now validate required arguments before caller cancellation, as their transport-base counterparts already do. They also reject a null task from the direct receive observer with the same operation-specific diagnostic as the transport base. Valid observer tasks are returned unchanged: identity, pending completion, original faults and original cancellation tokens remain observable. Delivery/fault state is recorded before the corresponding callback; observer failure does not undo that state.

Eight handwritten requirement-bound test methods exercise both actual receive-context implementations, all three notification operations and their applicable argument, cancellation, completion and metadata boundaries. The first successful focused run after fixture corrections executed all 48 cases: 48 passed, zero failed, skipped, pending or other; native exit 0. This is bounded receive-layer evidence, not proof that every API, parameter, feature or productive assembly is fully tested or A+.

The final restored compiler completed with zero warnings/errors, and the fresh unfiltered native Core run passed all 4059 cases with native exit 0. The exact multiset of all 4011 previous case names is retained; the only added cases are the 48 new receive-contract cases. All eight actual compiled counterchanges were killed at the intended oracle and restored exactly. The original full-source/API/architecture goal remains active. No feature or signature was removed; no compatibility alias, warning suppression, conditional-compilation workaround or generated comment was introduced.

## Authority and owning-test admission

The current governance inputs and hash-bound slice were checked before productive edits. The changed `DECISIONS.md` was personally read in full through line 1696; current SHA256 is `53fad5475972643d42fd653e8d485eda79e488c5193ce6b7e02779b53c74043b`. The ServiceBus slice authority remains unchanged; unrelated licensing instructions were not used as ServiceBus authorization.

Current owning-graph admission completed successfully at the parent commit: 622 exact inputs comprising Core 558, shared support 43 and build/project graph 21; sorted path/content SHA256 `ead67a273cf3f065cb35e458a7165562ba76d55050a936bf81ca560334c87ab5`. All 617 unchanged inputs retain the prior personal full-read admission. The five changed parent inputs were personally read in iteration 132. The current admission also parsed the requirement JSON in full and validated all 20 XML inputs. New test code and its eight appended requirement rows were personally read/checked in this iteration; no verdict depends on opening excluded review, TestResults or legacy trees.

The full ledger now has 2933 unique requirement/variant keys. All 2925 parent rows remain a semantically identical prefix; exactly eight rows were appended for `REQ-VSB-RECEIVE-NOTIFICATION`, with the exact owning assembly, type and method names below. Source-to-test binding is additionally subject to the final unfiltered Core run.

## Requirement-to-test evidence

All rows use type `ViciOne.ServiceBus.Tests.Transports.Receiving.ReceiveNotificationContractTests` in assembly `ViciOne.ServiceBus.Tests`. The clean focused native run was the freshly compiled Release owning executable, not an earlier DLL.

| Requirement variant | Exact test method | Cases | Clean focused evidence |
| --- | --- | ---: | --- |
| arguments-before-cancellation-without-side-effects | `InvalidArguments_AreRejectedBeforeCancellationWithoutChangingState` | 4 | 4 passed |
| caller-cancellation-preserves-token-and-state | `CanceledNotification_PreservesTheCallerTokenAndHasNoSideEffectsAsync` | 6 | 6 passed |
| held-observer-task-and-exact-arguments | `HeldObserverTask_IsForwardedWithExactArgumentsAndDeliveryStateAsync` | 6 | 6 passed |
| null-observer-task-has-operation-specific-diagnostic | `NullObserverTask_IsRejectedWithAnOperationSpecificDiagnostic` | 6 | 6 passed |
| observer-task-fault-and-cancellation-remain-original | `FaultedOrCanceledObserverTask_PreservesTheOriginalOutcomeAsync` | 12 | 12 passed |
| synchronous-observer-fault-remains-original | `ThrowingObserver_PropagatesTheOriginalFailureAfterRecordingState` | 6 | 6 passed |
| existing-consumer-fault-metadata-is-preserved | `ConsumerFaultNotification_PreservesExistingFaultMetadataAsync` | 2 | 2 passed |
| connected-null-observer-remains-a-faulted-task | `ConnectedNullObserver_ReturnsFaultedTaskWithoutLosingDeliveryStateAsync` | 6 | 6 passed |

Argument tests include null, empty and whitespace consumer identities and multiple-invalid-input guard order, with and without pre-cancellation. They assert exact exception types/parameter names, unchanged flags, no fault payload and no observer call. Valid pre-cancellation retains the caller token and has no observable delivery effect. A genuinely held observer task is released in `finally` and boundedly awaited using a non-cancelled cleanup token. Cancelling the caller after invocation does not complete the held observer task.

The observer records flags, fault-payload identity and exact metadata strings at callback entry, separately from final-state checks. Fresh message metadata is compared with the independent literal `ViciOne.ServiceBus.Tests.Transports.Receiving.ReceiveNotificationContractTests+NotificationMessage`, not with the same production formatter used to construct it. Existing fault metadata is checked against the exact seed instance and its two original values. Unexpected pre-/post-receive callbacks throw instead of silently succeeding.

Direct null observer tasks are synchronous operation-specific errors. A connected null-returning observer instead produces the non-null faulted task already guaranteed by `Connectable`, with its own connection-callback diagnostic. These are separate boundaries; the new direct Mediator guard is not credited with behavior already provided by the connected aggregate.

The fixture constructs real `MediatorReceiveContext`, a thin `BaseReceiveContext` subclass, real send/serialization/consume contexts, the canonical metadata codec and an actual receive observable. Send/publish/topology dependencies are strict test proxies, not executed provider acceptance. The transport endpoint exposes only setup-required metadata/observer/payload/publish-provider access; every actual provider operation remains unexpected. These tests are not real broker, database, cloud or durable-sender acceptance.

## Structured diagnosis and internal counterreview

The first compiler rejected the test-only header-provider argument: the provider requires `SendHeaders`, not a raw dictionary. The fixture now uses the actual `DictionarySendHeaders` implementation. A later compiler rejected application `ConsumeContext<T>.ReceiveContext`; the correct existing access is `context.Advanced().ReceiveContext`. Neither failed compilation was followed by a test run against a stale DLL.

The first fresh native run was 24 passed/24 failed: all transport-base fixtures failed during construction before their assertions, while all Mediator cases passed. Personally reading the deserializer and base consume constructors established that `PublishEndpointProvider` is required during construction. Supplying that getter with a real strict provider corrected setup; no behavioral assertion was relaxed. The next successful warnings-as-errors compiler and fresh native run established 48/48 passing.

An explicitly authorized internal Sol counterreview read the source closure and all eight methods/48 cases; it was not external product-team acceptance. It found two Medium oracle gaps: fresh fault metadata was checked only for presence, and unexpected lifecycle callbacks were silently accepted. Both were manually corrected. The additional callback-state snapshots cover temporal ordering rather than merely state by method return. The reviewer also caught the advanced-facade compile access and reviewed the corrected fixture and expanded documentation closure. The amended bounded static review reports no remaining concrete finding; dynamic evidence remains the lead's responsibility.

## Causal compiled counterchanges

Each counterchange is a handwritten, temporary source edit in `MediatorReceiveContext.cs`, with no test edit. Its owning-project compiler must actually finish with exit 0 before native execution. The report distinguishes a killed mutant from a compilation/setup failure. After each native result the source is manually restored and independently checked against candidate SHA256 `4f298c6762e1f656390d537510ce9bce6a25196e953c2e008e17e37affeabb8d` before starting another counterchange.

| Counterchange | Exact selected test method | Executed / passed / failed | Actual outcome |
| --- | --- | --- | --- |
| M01: pre-cancellation bypasses successful-consume argument guards | `InvalidArguments_AreRejectedBeforeCancellationWithoutChangingState` | 4 / 3 / 1 | Compiler 0; native 2; intended cancelled Mediator case no longer throws `ArgumentNullException`; restored SHA exact |
| M02: direct post-consume null task is no longer rejected | `NullObserverTask_IsRejectedWithAnOperationSpecificDiagnostic` | 6 / 5 / 1 | Compiler 0; native 2; intended Mediator consume case no longer throws `InvalidOperationException`; restored SHA exact |
| M03: fresh consumer fault records `wrong-consumer` | `HeldObserverTask_IsForwardedWithExactArgumentsAndDeliveryStateAsync` | 6 / 5 / 1 | Compiler 0; native 2; intended Mediator consumer-fault case rejects wrong literal consumer metadata; restored SHA exact |
| M04: fresh consumer fault records `wrong-message` | `HeldObserverTask_IsForwardedWithExactArgumentsAndDeliveryStateAsync` | 6 / 5 / 1 | Compiler 0; native 2; intended Mediator consumer-fault case rejects wrong literal message metadata; restored SHA exact |
| M05: consume notification also invokes pre-receive | `HeldObserverTask_IsForwardedWithExactArgumentsAndDeliveryStateAsync` | 6 / 5 / 1 | Compiler 0; native 2; intended Mediator consume case rejects the unexpected lifecycle callback; restored SHA exact |
| M06: successful-delivery flag is recorded only after the callback, in `finally` | `ThrowingObserver_PropagatesTheOriginalFailureAfterRecordingState` | 6 / 5 / 1 | Compiler 0; native 2; original observer exception and final state remain intact, but intended callback snapshot rejects false delivered state; restored SHA exact |
| M07: consumer fault replaces pre-existing fault metadata | `ConsumerFaultNotification_PreservesExistingFaultMetadataAsync` | 2 / 1 / 1 | Compiler 0; native 2; intended Mediator case rejects replacement of the seed instance; restored SHA exact |
| M08: successful-consume observer task is replaced with `Task.CompletedTask` | `HeldObserverTask_IsForwardedWithExactArgumentsAndDeliveryStateAsync` | 6 / 5 / 1 | Compiler 0; native 2; intended Mediator consume case rejects completed replacement of the held observer task; restored SHA exact |

All eight compiled counterchanges were killed at their intended behavioral assertion: 42 executed cases, 34 passed and eight intentionally failed, with zero skipped, pending or other. M03 and M04 independently demonstrate the two exact-value oracles requested by the internal counterreview. M05 independently demonstrates the strict lifecycle oracle. M06 demonstrates that callback-entry snapshots detect wrong temporal order even when the old final-state/exception assertions would succeed. M07 and M08 independently exercise retained metadata identity and asynchronous task identity/lifetime. No global mutation score or universal test efficacy is inferred.

## Personal source reading and manual comments

The lead personally read 26 productive source files, 3616 lines at the final candidate. The source bodies were read through EOF, not inferred from searches or delegated summaries. Inventory/count/hash checks are read-only; they did not author source, comments or report prose. All comment changes were written manually after understanding the corresponding implementation.

Final read/source verification independently checked all 26 paths, line counts and SHA256 values against the fully read candidate: all equal, verification exit 0. Ordered path/line/hash manifest SHA256: `9d832bd69b77f13ea57e3399e64d56b20b7f9f21c7ffedeac58810044a8dfdf0`.

The changed source/comment files are `MediatorReceiveContext`, `BaseReceiveContext`, the advanced `ReceiveContext` contract, `IReceiveObserver`, `ReceiveObservable`, `DictionarySendHeaderProvider`, `AdvancedConsumeContextExtensions`, `ConsumeContext` and `BaseConsumeContext`. Contract comments distinguish pre-invocation notification cancellation from already-started observer work, direct tasks from the connected aggregate, receive settlement from consumer completion, and receive-only reporting from infrastructure consume reporting that can include fault generation.

Existing generic/non-generic type families remain co-located; nested fixture/helper types belong to their contract-test class. Task-returning notification and observer methods have `Async` names; synchronous guard tests and snapshot helpers do not. This is local naming/structure review, not the required eventual whole-fork bidirectional method audit.

### Complete personal source-read manifest

Paths are repository-relative. Counts include an unterminated final physical line where applicable. These are per-iteration reads, not 26 previously unread files or a whole-source completion percentage. The final candidate hashes below, the owning commit and its annotated tag bind this read manifest to the secured source.

| Personally fully read source | Lines |
| --- | ---: |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorReceiveContext.cs` | 185 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSerializationContext.cs` | 101 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorConsumeContext.cs` | 100 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSendMessageContext.cs` | 48 |
| `src/ViciOne.ServiceBus.Abstractions/ConsumeContext.cs` | 195 |
| `src/ViciOne.ServiceBus.Abstractions/Observers/IReceiveObserver.cs` | 45 |
| `src/ViciOne.ServiceBus.Abstractions/Util/Connectable.cs` | 201 |
| `src/ViciOne.ServiceBus/Serialization/Metadata/ServiceBusMetadataJson.cs` | 26 |
| `src/ViciOne.ServiceBus.Abstractions/Observers/Observables/ReceiveObservable.cs` | 61 |
| `src/ViciOne.ServiceBus.Abstractions/Middleware/ScopePipeContext.cs` | 131 |
| `src/ViciOne.ServiceBus/Serialization/Headers/DictionarySendHeaders.cs` | 148 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/TypeCache.cs` | 87 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/AdvancedConsumeContextExtensions.cs` | 96 |
| `src/ViciOne.ServiceBus.Abstractions/Internals/Reflection/TypeNameFormatter.cs` | 98 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/ConsumerFaultContext.cs` | 10 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/ReceiveContext.cs` | 80 |
| `src/ViciOne.ServiceBus.Abstractions/Transports/DictionarySendHeaderProvider.cs` | 34 |
| `src/ViciOne.ServiceBus/Context/Consumption/BaseConsumeContext.cs` | 396 |
| `src/ViciOne.ServiceBus/Context/Consumption/DeserializerConsumeContext.cs` | 75 |
| `src/ViciOne.ServiceBus/Transports/Receiving/BaseReceiveContext.cs` | 275 |
| `src/ViciOne.ServiceBus/Transports/Receiving/ReceiveEndpointContext.cs` | 98 |
| `src/ViciOne.ServiceBus/Transports/Receiving/ReceivePipeDispatcher.cs` | 319 |
| `src/ViciOne.ServiceBus/InMemoryTransport/Runtime/InMemoryReceiveEndpointContext.cs` | 89 |
| `src/ViciOne.ServiceBus/Transports/Sending/MessageSendContext.cs` | 399 |
| `src/ViciOne.ServiceBus/InMemoryTransport/Runtime/InMemoryReceiveContext.cs` | 56 |
| `src/ViciOne.ServiceBus/Transports/Diagnostics/ReceiveEndpointLoggingExtensions.cs` | 263 |
| **Total** | **3616** |

## Adjacent open work

- `BaseConsumeContext.NotifyFaultedAsync<T>` may generate a fault before forwarding a pre-cancelled notification token to the receive layer. The 48 receive-layer cases do not establish the consume-layer cancellation/fault-generation policy.
- The base receive constructor allocates its cancellation source before rejecting a missing endpoint input address. Ownership on partial construction needs separate causal inspection/testing; a resource leak is not declared measured here.
- A matching consume-token cancellation is logged differently by the base and Mediator receive fault implementations. The logger outcome/policy has not yet been causally tested in this package.
- The prior cache, writer reservation/admission, support lifetime, paired-ledger and provider-acceptance worklists remain open except for individually closed findings in earlier evidence. Whole-source comments, complete API/parameter coverage, overall fork coverage, complete legacy/dummy/directive removal and global naming/structure checks are not certified by this iteration.

## Raw provenance and continuation

### Final restored execution and coverage

The owning Release project was compiled after the final exact source restoration: exit 0, zero warnings/errors, 6.60 seconds. The subsequently executed owning native DLL passed 4059/4059 cases in 25.357 seconds; zero failed, skipped, pending or other. No class/method filter was applied. The runner's five-minute timeout, strict zero-tests policy, minimum 4059 and fail-skips/fail-warns were enabled. An independent full CTRF parse verifies the exact 48 new receive cases and 4011 retained cases; a separate comparison with iteration 132 verifies the complete previous case-name multiset, with no missing or unexpected retained case.

Native CodeCoverage 18.10.0 collection occurred in that same unfiltered run. The manually written XML settings are byte-identical to iteration 132's settings: exactly the nine observed productive Core modules, all auto-properties included, default attribute exclusions disabled, test assemblies excluded and only dynamic managed instrumentation enabled. XML configuration and the generated Cobertura document both parse completely. This is native collection through the run-tests path, not a source/comment generator, report generator or CRAP pipeline.

Weighted observed-module line coverage is **50617/64571 = 78.3897%**. Weighted observed-module branch coverage is **17462/24304 = 71.8483%**. These denominators exclude productive persistence/scheduling/transport adapters outside this owning Core module closure. They must not be presented as whole-ServiceBus/fork coverage or 100% API/parameter testing. No current whole-product CRAP result or guarantee of universal correctness is made.

| Observed productive module | Line % | Branch % |
| --- | ---: | ---: |
| ViciOne.ServiceBus | 80.3292 | 74.0449 |
| ViciOne.ServiceBus.Abstractions | 59.4224 | 53.7049 |
| ViciOne.ServiceBus.Courier | 89.1005 | 75.4425 |
| ViciOne.ServiceBus.Futures | 90.7023 | 85.4839 |
| ViciOne.ServiceBus.Initializers | 100.0000 | 100.0000 |
| ViciOne.ServiceBus.JobService | 95.6283 | 89.7525 |
| ViciOne.ServiceBus.Mediator | 91.0675 | 78.6624 |
| ViciOne.ServiceBus.Sagas | 64.2705 | 57.8763 |
| ViciOne.ServiceBus.Testing | 94.3834 | 77.9841 |

### Final candidate and raw-result hashes

| Input or result | SHA256 |
| --- | --- |
| Restored MediatorReceiveContext.cs | `4f298c6762e1f656390d537510ce9bce6a25196e953c2e008e17e37affeabb8d` |
| ReceiveNotificationContractTests.cs | `bd4ef023822bbff602944d4efef7c9a226b561719b05b15320af15483933f207` |
| CoreRequirements.json | `1c34657025cc59b543a74d03f89aaad3064002c5c690a9e650b7d8b0b35b8476` |
| Loaded owning native test DLL | `f2c9c575e86b7147e25232f819412e752e0dc53c118e2139a2c9ffe2b6de96f8` |
| Loaded owning Mediator DLL copy | `80c8c8c0ccedf76804dbb7b4e4323f65941d4d83dbac0b058e714076b4351b58` |
| Final Core CTRF | `35eec52e08f5d4a4a61f822d052db9ba77580fb389abea0744202706cd901924` |
| Final Cobertura | `e0f4993e9c9f70c4a3cfa2a676d311831820090dfb760ea032f23493dabf6192` |
| Collector settings | `c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13` |
| Final restored build log | `34d4cd75217837df7fdbc136b4932b678f132b02cc593ea5e3d2ef0cceef4991` |
| Final native log | `c30c84f8f0f860fcdd3a8b47e9cdbc2738713b1e6ee8cbfcdb481e869c0136a4` |

Raw records are isolated under `/private/tmp/vsb-iteration133-receive-notification.YLl6Yh`. `healthy-build.log` and `healthy-build-v3.log` contain the two explicit failed compiler diagnoses. `healthy-build-v2.log`, `healthy-build-v4.log` and `healthy-build-v5.log` are successful compilers for their respective candidate revisions. `healthy-focused.log` / its CTRF file preserve the first fresh fixture failure; `healthy-focused-v2.log` / its CTRF file preserve the clean final-candidate 48-case result. Every MSBuild invocation has its own `prefix-{}.binlog`. Native results use explicit isolated result directories, strict zero-test policy, minimum expected counts, fail-skips/fail-warns and bounded runner timeouts.

The scoped commit must include only the nine personally read productive source/comment files, the handwritten contract test, its eight ledger bindings and this report. The new annotated tag names this receive-contract remediation, not completion of the entire A+ goal. The known destination is the same private admin-controlled `EdgarLiebold/vicione-servicebus` repository; normal branch push triggers its existing native-tests workflow, while the tag has no additional publication trigger. Normal remote backup must independently confirm branch, tag object and peeled commit; an accepted push alone is not reported as verification. Excluded trees and unrelated work remain untouched.
