# Iteration 135 — Endpoint resolution contract remediation

Date: 2026-09-16.
Branch: `feature/servicebus-a-plus-api`.
Parent: `b61997f2ceebaec0b64efdcf08d55274f6140d58`.
Development slice: `WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`.
Slice SHA256: `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.

## Outcome and scope

Response, message-fault and receive-fault resolution now reject a null provider task and a null resolved endpoint explicitly, in both immediate and pending paths. Diagnostics identify publication versus addressed sending, and send diagnostics contain the actual destination. Required arguments precede caller cancellation. Pre-cancellation performs no provider invocation; once a provider has started, the actual task is owned without cancellation-only detachment, and its original exception or token remains the outcome.

The existing routing and decoration rules are retained: explicit fault address precedes response address; response-only faults fall back to that response address; unaddressed outcomes are published using the exact response/fault contract. Explicit response/fault request IDs override the consumed ID, and omitted IDs inherit it. Consume-aware endpoints retain the exact underlying endpoint, consume scope, headers, causality and opted-in request lifetime. A receive fault without a consume scope retains the original undecorated endpoint. No feature, public signature, alias or compatibility layer was added or removed.

The new fixture also executes the actual default fault-generation extension, fault event and fault pipe. It does not override protected generation. Pending sends must finish before receive notification; a later caller cancellation does not detach dispatch, but can cancel the subsequent receive phase. Disabled publication still permits explicitly addressed faults and response-address fallback, while an unaddressed failure notifies receive without publication. The controlled provider/send boundaries are not broker/database/cloud or durable-provider acceptance.

Clean focused execution passes all 155 cases. All twelve compiled causal counterchanges are detected and exactly restored. The final unfiltered Core run passes all 4248 cases, preserving the exact multiset of all 4093 parent cases and adding precisely these 155. The same native run measures the exact nine productive Core modules: 78.4003% line coverage and 71.8938% branch coverage. The original entire-source A+ API/architecture goal remains active. This package is not a claim of universal correctness, all API/parameter testing, full-fork coverage or external independent acceptance.

## Research, owning admission and structured diagnosis

The previous turn is progress: iteration 134 was committed, annotated-tagged, atomically pushed and independently verified against exactly the branch, tag object and peeled commit. No settled directory decision was reopened. Applicable governance and the hash-bound slice retain their previously completed personal readings at unchanged hashes.

Before test edits, all 624 owning inputs were revalidated against the parent commit, with exact working Git blob equality. The existing 560 Core inputs and unchanged 64 support/build inputs retain their prior full personal readings. The new handwritten test was personally read in full before execution; its final cleanup amendment was separately read. The final owning set has 625 inputs: Core 561 plus support/build 64. Sorted path/content manifest SHA256: `fe3699ebddeccacaef21194e020acdc1d3f91ce00799849cfcb51aedac6678af`.

The complete ledger parses, preserves all 2942 parent rows as an identical semantic prefix, and has exactly eleven manually appended unique requirement/variant keys. Current ledger count: 2953. Each new row agrees with the exact test method and its explicit RequirementCoverage attribute. Shared test foundations, project/package/compiler settings and workflow verdicts are unchanged. Excluded review, TestResults and legacy trees are not opened or modified.

The first compiler failed only in the new test fixture: it incorrectly treated typed application ConsumeContext as the advanced infrastructure contract, assigned immutable SentTime and dereferenced the not-yet-initialized receive property. The actual APIs/source were read, and the fixture was corrected to use Advanced(), freeze the actual initial send timestamp with a real FakeTimeProvider, and guard its initialization. Response causality asserts InitiatorId, not fault-correlation semantics. No native test was run after that failed compiler, and no product API was changed to accommodate the fixture.

The corrected baseline compiler exited 0 with zero warnings/errors, 26.22 seconds. Its unchanged-product native run executed 118 cases: 81 passed and 37 failed at intended null boundaries, exit 2. Complete CTRF parsing locates exactly eleven null-task cases, 22 immediate/pending null-result cases, and four default fault-route null-boundary cases; no unrelated failure, skip, pending or other outcome occurred. No assertion was relaxed to obtain a passing product correction.

The first corrected cohort compiled with zero warnings/errors in 62.68 seconds and passed 151/151. The final request-ID fallback extension compiled in 34.80 seconds and passed 155/155. Before counterchanges, one cleanup-only line explicitly drained Owner.ConsumeCompleted after releasing the held default dispatch and observing notification. This additionally observes actual registered sends for deliberately detached generation implementations. The final candidate-v2 compiler and 155/155 native run exited 0, with zero compiler warnings/errors, 27.91 seconds build and 1.884 seconds native execution. Counterchanges use final test SHA256 `235bf8437dd0670e51c5c394b4ec4bb7094dd05c1b7daef1b9bb651a2087e385`.

One M01 native attempt used a short method filter and correctly failed the unchanged minimum-test gate: zero tests, exit 8. The actual runner help requires a fully qualified method name. Only that filter was corrected; the same successful mutant compiler was reused, without rebuilding, restoring, skipping or relaxing a gate. All subsequent method filters use `ViciOne.ServiceBus.Tests.Context.Consumption.EndpointResolutionContractTests.` followed by the exact method name.

## Requirement-to-test evidence

All methods belong to `ViciOne.ServiceBus.Tests.Context.Consumption.EndpointResolutionContractTests`, assembly `ViciOne.ServiceBus.Tests`. The final candidate-v2 clean focused run exits 0 and supplies each row below. These are handwritten behavioral oracles, not source-name pairing or a coverage-only inference.

| Requirement variant | Exact test method | Cases | Clean focused result |
| --- | --- | ---: | --- |
| every-route-rejects-null-resolution-task-synchronously | `NullResolutionTask_IsRejectedSynchronouslyOnEveryRoute` | 11 | 11 passed |
| every-route-rejects-null-endpoint-in-fast-and-held-paths | `NullEndpoint_IsRejectedForImmediateAndHeldResolutionAsync` | 22 | 22 passed |
| every-route-pre-cancellation-has-no-provider-effects | `PreCanceledResolution_NeverInvokesEitherProviderAsync` | 11 | 11 passed |
| every-route-owns-held-resolution-and-original-outcome | `EveryRoute_OwnsHeldResolutionAndPreservesItsOriginalOutcomeAsync` | 33 | 33 passed |
| every-route-preserves-endpoint-request-metadata-and-consume-ownership | `EveryRoute_DecoratesTheExactEndpointAndTransfersRequestMetadataAsync` | 22 | 22 passed |
| every-route-preserves-immediate-provider-exception-or-cancellation | `ImmediateProviderFailure_PreservesTheOriginalExceptionOrCancellationAsync` | 33 | 33 passed |
| all-public-required-arguments-win-before-cancellation | `RequiredArguments_AreRejectedBeforeCancellation` | 2 | 2 passed |
| explicit-address-with-omitted-request-id-inherits-consumed-request | `ExplicitAddress_WithOmittedRequestIdInheritsTheConsumedRequestAsync` | 4 | 4 passed |
| default-fault-generation-owns-dispatch-before-receive-notification | `DefaultFaultGeneration_AwaitsTheActualDispatchBeforeReceiveNotificationAsync` | 6 | 6 passed |
| default-fault-route-failure-never-notifies-receive | `DefaultFaultRouteFailure_NeverNotifiesReceiveAsync` | 8 | 8 passed |
| disabled-publication-preserves-explicit-fault-and-response-routing | `DisabledFaultPublication_PreservesAddressedFaultsWithoutPublishingUnaddressedFaultsAsync` | 3 | 3 passed |

The eleven routing shapes cover inferred and explicit response addresses; fault-address priority and response-only fault fallback; explicit message faults; addressed receive faults with both addresses and with response alone; and publication for response, message fault, receive fault with consume scope and receive fault without it. Routes 2 and 5 carry both distinct fault and response addresses, so exact destination assertions distinguish reversed precedence.

Argument tests combine missing context and missing explicit address, live/pre-cancelled callers, exact parameter names and no provider/send/observer effects. Pending resolution tests independently hold real tasks, cancel the caller, and then release success, the original exception instance or an independent provider cancellation token. They assert root completion/status and exact provider invocation, address, contract and token. Metadata sends verify request-ID override, inherited request-ID fallback, exact 37-second lifetime, InitiatorId, application header, actual consume payload and incomplete consume completion until real dispatch release.

Default generation tests inspect the actual Fault message, original message and identifier, frozen timestamp, nonempty fault identity, host, supported contract names and exception snapshot. They hold the actual send and inspect receive state before observer entry. Provider boundary failures never notify receive or add consumer-fault state. All controlled tasks are released in finally; cleanup uses a bounded non-cancelled token and only swallows already-settled failures, never an incomplete timeout. No sleep or blocking task result is an asynchronous oracle.

## Internal counterreview and causal counterchanges

An explicitly authorized internal Sol reviewer initially read the bounded source and 621-line test candidate and identified a Medium missing both-address priority oracle and a Low omitted explicit request-ID fallback oracle. Both were corrected without weakening existing assertions. The reviewer then read the whole corrected 712-line candidate and eleven bindings, matching all seven candidate hashes; remaining concrete static findings were zero at that candidate. General then typed send-pipe application agrees with the actual transport sequence, and protected generation is genuinely not overridden. The later one-line cleanup drain changes no case/attribute/assertion and is exercised by candidate-v2. This is internal static review, not independent external acceptance or reviewer-executed tests.

The same internal reviewer then personally re-read all 713 lines of the final test, including every fixture, helper and comment, and independently verified final test SHA256 `235bf8437dd0670e51c5c394b4ec4bb7094dd05c1b7daef1b9bb651a2087e385` and ledger SHA256 `92d518bf424c2ab9eaf830777035b41004cc2400c1fea5cae182d89bad8a7006` against the exact parent. Remaining concrete test findings: zero. Product source was deliberately not rebound while temporary counterchanges ran; no native execution is attributed to the reviewer.

The proof boundary remains explicit: the metadata cohort verifies positive 37-second request lifetime, not the expired one-second floor, absence of a request ID or ordinary-send lifetime opt-out. Default fault generation is executed, but retry/redelivery headers, outbox bypass and every Host/ExceptionInfo field are not exhaustively established by these cases. These adjacent capabilities require their own existing or additional evidence before any broader certification.

After the twelfth restoration, the internal Sol reviewer personally re-read the entire endpoint extension, both endpoint-provider interfaces, ISendContextPipe and BaseSerializerContext, compared the exact parent and rebound all seven final source/test/ledger hashes. No remaining concrete product finding was identified in this bounded candidate. The source review confirms unchanged public signatures/defaults/constraints and preserved fault routing, request metadata, actual generation, delivery token, owned awaits, outbox bypass and retry/redelivery logic. This remains internal static review; all execution evidence belongs to the Lead's actual runs.

Each counterchange is a separately handwritten temporary source patch with unchanged final tests. The shell success gate permits native execution only after the owning compiler actually succeeds. Every compiler succeeds with zero warnings/errors and its own retained unique binlog; every valid native counterchange run exits 2. Every run has zero skipped, pending or other cases. After every result, manual restoration independently matches endpoint source SHA256 `9824e8e502699b87907a1962836616613e41d06df2962178597e73b9f893a9c7` before another patch. Final unchanged test SHA256 is `235bf8437dd0670e51c5c394b4ec4bb7094dd05c1b7daef1b9bb651a2087e385`.

| Counterchange | Actual temporary behavior | Exact method | Cases | Passed | Intentionally failed |
| --- | --- | --- | ---: | ---: | ---: |
| M01 | Omit publish null-task diagnostic | `NullResolutionTask_IsRejectedSynchronouslyOnEveryRoute` | 11 | 7 | 4 |
| M02 | Omit send null-task diagnostic | `NullResolutionTask_IsRejectedSynchronouslyOnEveryRoute` | 11 | 4 | 7 |
| M03 | Omit immediate publish null-endpoint diagnostic | `NullEndpoint_IsRejectedForImmediateAndHeldResolutionAsync` | 22 | 18 | 4 |
| M04 | Omit held publish null-endpoint diagnostic | `NullEndpoint_IsRejectedForImmediateAndHeldResolutionAsync` | 22 | 18 | 4 |
| M05 | Omit immediate send null-endpoint diagnostic | `NullEndpoint_IsRejectedForImmediateAndHeldResolutionAsync` | 22 | 15 | 7 |
| M06 | Omit held send null-endpoint diagnostic | `NullEndpoint_IsRejectedForImmediateAndHeldResolutionAsync` | 22 | 15 | 7 |
| M07 | Reverse fault-address priority in message and receive fault resolution | `EveryRoute_DecoratesTheExactEndpointAndTransfersRequestMetadataAsync` | 22 | 18 | 4 |
| M08 | Omit consumed request-ID fallback for both explicit-address APIs | `ExplicitAddress_WithOmittedRequestIdInheritsTheConsumedRequestAsync` | 4 | 0 | 4 |
| M09 | Disable request-lifetime inheritance for response/fault send and publish resolution | `EveryRoute_DecoratesTheExactEndpointAndTransfersRequestMetadataAsync` | 22 | 2 | 20 |
| M10 | Detach held send resolution on caller cancellation | `EveryRoute_OwnsHeldResolutionAndPreservesItsOriginalOutcomeAsync` | 33 | 12 | 21 |
| M11 | Detach held publish resolution on caller cancellation | `EveryRoute_OwnsHeldResolutionAndPreservesItsOriginalOutcomeAsync` | 33 | 21 | 12 |
| M12 | Start actual default fault dispatch without awaiting it | `DefaultFaultGeneration_AwaitsTheActualDispatchBeforeReceiveNotificationAsync` | 6 | 0 | 6 |
| Total | Twelve distinct compiled causal counterchanges | | 230 | 130 | 100 |

Complete CTRF summaries and exact selected method membership agree with every row. Failure-message inspection distinguishes removed explicit null diagnostics, the reversed actual URI, the missing actual request ID, the missing 37-second lifetime and premature root completion. Each counterchange has at least one causal behavioral failure and is detected; this is not a full-fork mutation score. M01 uses its corrected fully qualified filter report, while the initial zero-case exit-8 attempt remains separate diagnostic evidence.

The final internal report audit found one Low provenance issue: the initial 621-line review and subsequent corrected 712-line review were conflated. The chronology above was manually corrected. The reviewer independently reconciled baseline, clean focused, all twelve counterchange and final Core reports, exact parent name-count retention, full collector XML/module fractions, settings and artifact hashes. No remaining concrete source, test or report finding was identified in this bounded audit. The reviewer did not execute native tests or claim personal reading of the entire 625-input owning admission set or the 29-file Lead source inventory; those readings retain their explicitly attributed Lead evidence.

## Personal source reading, comments and structure

The connected read inventory now contains 29 productive files / 4406 current lines, including the retained eleven full readings from iteration 134 and eighteen additionally fully read connected contracts, adapters, serialization, metadata, fault-generation and actual transport send files. Sorted path/line-count/content manifest SHA256: `a82b88fcf3127b14607b811c20306eac895318a14bd6ac4d0a82c891b8d8585f`. Its canonical rows are sorted UTF-8 path, TAB, current newline count, TAB, file SHA256, LF. Hashing only verifies current bytes; it is not credited as source reading. This is a bounded connected inventory, not proof that the entire fork has already been read.

Every file below was personally read in full by the Lead. Retained readings were revalidated at unchanged bytes; owned comment corrections followed code understanding. The explicit paths allow later iterations to consolidate reading coverage without counting a repeated file twice.

| Fully read productive file, repository-relative | Current lines |
| --- | ---: |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/AdvancedSendEndpointExtensions.cs` | 107 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/ReceiveContext.cs` | 80 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/SendContextExtensions.cs` | 210 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Contexts/SerializerContext.cs` | 70 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/IAdvancedSendEndpoint.cs` | 97 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/IPublishEndpointProvider.cs` | 15 |
| `src/ViciOne.ServiceBus.Abstractions/ConsumeContext.cs` | 197 |
| `src/ViciOne.ServiceBus.Abstractions/ISendEndpointProvider.cs` | 15 |
| `src/ViciOne.ServiceBus.Abstractions/Transports/ISendContextPipe.cs` | 15 |
| `src/ViciOne.ServiceBus.Abstractions/Transports/PublishEndpoint.cs` | 239 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorConsumeContext.cs` | 100 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSendMessageContext.cs` | 48 |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSerializationContext.cs` | 101 |
| `src/ViciOne.ServiceBus/Advanced/ConsumeContextEndpointExtensions.cs` | 281 |
| `src/ViciOne.ServiceBus/Advanced/LogContext.cs` | 318 |
| `src/ViciOne.ServiceBus/Context/Consumption/BaseConsumeContext.cs` | 419 |
| `src/ViciOne.ServiceBus/Context/Consumption/DeserializerConsumeContext.cs` | 75 |
| `src/ViciOne.ServiceBus/Context/Consumption/MessageConsumeContext.cs` | 386 |
| `src/ViciOne.ServiceBus/Events/Faults/FaultEvent.cs` | 109 |
| `src/ViciOne.ServiceBus/Middleware/InternalOutboxExtensions.cs` | 32 |
| `src/ViciOne.ServiceBus/Serialization/Contexts/BaseSerializerContext.cs` | 160 |
| `src/ViciOne.ServiceBus/Transports/Receiving/BaseReceiveContext.cs` | 275 |
| `src/ViciOne.ServiceBus/Transports/Sending/ConsumeSendEndpoint.cs` | 69 |
| `src/ViciOne.ServiceBus/Transports/Sending/ConsumeSendPipeAdapter.cs` | 65 |
| `src/ViciOne.ServiceBus/Transports/Sending/ITransportSendEndpoint.cs` | 19 |
| `src/ViciOne.ServiceBus/Transports/Sending/MessageSendContext.cs` | 399 |
| `src/ViciOne.ServiceBus/Transports/Sending/SendContextPipeAdapter.cs` | 62 |
| `src/ViciOne.ServiceBus/Transports/Sending/SendEndpoint.cs` | 262 |
| `src/ViciOne.ServiceBus/Transports/Sending/SendEndpointProxy.cs` | 181 |
| Total | 4406 |

Comments were manually corrected after reading and understanding code: provider task/result nullability, precise resolution cancellation semantics, send-context configuration rather than transport dispatch, and removal of BaseSerializerContext's false universal null-to-empty dictionary claim (the actual mediator codec rejects null). No source, comment or report generator was used. No workflow/process prose, conditional-compilation workaround or warning suppression was added to product code.

Current filenames match the reviewed primary types and their existing capability namespaces/folders. Generic and non-generic FaultEvent share their named contract family. Task-returning public validation wrappers and private awaited resolvers keep Async suffixes; synchronous diagnostics, data providers and reflection dispatch do not. This is not the requested whole-fork bidirectional naming/filename/comment/directive/dummy/legacy certification, which remains part of the active larger goal.

## Final validation, provenance and continuation

After all twelve exact restorations, the fresh owning Release compiler exits 0 with zero warnings/errors, 62.80 seconds. Its shell success gate then runs the unfiltered owning Core executable with minimum 4248 cases, strict zero-case policy, fail-skips and fail-warns, a five-minute timeout, native CTRF reporting and native managed coverage. The combined command exits 0; the platform reports 4248 passed, zero failed/skipped, 19.781 seconds. The complete CTRF reports zero pending/other cases and a 16.607-second test-result interval. Native execution is never inferred from a surviving DLL after a failed compiler.

Complete parsing verifies the exact name multiset of every parent case, including duplicate names: all 4093 are retained. The eleven new exact methods contribute precisely 155 passed cases. Parent sorted case-name multiset SHA256: `f7da0431aa0b3334ee7406c4c17e10d367acaed3471d786050814564261b5151`. Current sorted case-name multiset SHA256: `0f620b07e402b556d5a99010f2ad1fdf73fc52392fe75a8010d8672a38ec4584`. Both hashes use the sorted name array encoded as compact JSON; passing old case retention is established by direct count-map equality, not hashes alone.

The collector XML parses and contains exactly the nine module identities configured below, with no test module. Weighted root coverage is 50638/64589 valid lines, 78.4003468083%, and 17486/24322 valid branches, 71.8937587369%; the root rates independently agree with these fractions. These are native Core measurements, not total fork or API/parameter completeness, and Cobertura complexity is not a computed CRAP score.

| Productive Core module | Line coverage | Branch coverage |
| --- | ---: | ---: |
| ViciOne.ServiceBus | 80.3560% | 74.1307% |
| ViciOne.ServiceBus.Abstractions | 59.4224% | 53.7049% |
| ViciOne.ServiceBus.Courier | 89.1005% | 75.4425% |
| ViciOne.ServiceBus.Futures | 90.7023% | 85.4839% |
| ViciOne.ServiceBus.Initializers | 100.0000% | 100.0000% |
| ViciOne.ServiceBus.JobService | 95.6283% | 89.7525% |
| ViciOne.ServiceBus.Mediator | 91.0675% | 78.6624% |
| ViciOne.ServiceBus.Sagas | 64.2306% | 57.8374% |
| ViciOne.ServiceBus.Testing | 94.3834% | 77.9841% |

Exact final commands, run from the repository root with the established .NET SDK IPC sandbox exception, without restore or compiler/project setting changes:

```text
TMPDIR='/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl' dotnet build tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -warnaserror '/bl:/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl/final-build-{}.binlog'
TMPDIR='/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl' dotnet exec artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests.dll --results-directory '/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl/final-core' --minimum-expected-tests 4248 --zero-tests-policy strict --fail-skips on --fail-warns on --progress off --timeout 5m --report-xunit-ctrf --report-xunit-ctrf-filename final-core.ctrf.json --coverage --coverage-output-format cobertura --coverage-output '/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl/final-core/final-core.cobertura.xml' --coverage-settings '/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl/core-productive-coverage.config'
```

The actual shell command redirects build/native output into the dedicated logs and connects these commands with `&&`. The final unique binlog exists at `final-build-20260916-003225--75600--5arimy.binlog`. All twelve counterchange compiler binlogs were also independently found; no compiler or report was overwritten by retry. No current whole-fork CRAP score or broker/database/cloud/durable-provider acceptance is asserted.

Final artifact SHA256 bindings:

| Measured artifact | SHA256 |
| --- | --- |
| Owning test-output ViciOne.ServiceBus.Tests.dll | `0d1ac1e31526ff3f2fd2700e80aab6d0fe4a99a94a31e92868477e3aee7395af` |
| ViciOne.ServiceBus.dll copied beside that owning test executable | `b04356563c6e4b0ea7dbde7e3a239521707524903083ff04941c3a487c85b148` |
| final-build.log | `4c9d90994669ecf07f4396d46e979956659701be2a57b086ea54c92e99014f2e` |
| final-core-native.log | `9236da194da52f1707118e56baa33ff84b460f60d3d115d6416ab2fbfa0c99d3` |
| final-core/final-core.ctrf.json | `c83799cfeb7be70f9d499cb647c0d93027c07821270849b02e0328c0a817bd42` |
| final-core/final-core.cobertura.xml | `040c1606356903a8f042737b8be5ed2ff1cbbb96e25c36990a24db55ab4645f3` |
| core-productive-coverage.config | `c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13` |

The final 625-input owning admission manifest was independently recomputed after all restorations and still equals `fe3699ebddeccacaef21194e020acdc1d3f91ce00799849cfcb51aedac6678af`. Its canonical rows are sorted UTF-8 path, TAB, file SHA256, LF. Final test bytes remain unchanged through focused candidate-v2, every causal counterchange and restored unfiltered execution. Temporary counterchange source deliberately differs; each restoration recovers the exact candidate source hash before the next patch and before final unfiltered execution. Scoped diff checking and normal Git checkpoint verification follow without changing measured product/test bytes.

Raw telemetry is isolated under `/private/tmp/vsb-iteration135-endpoint-contract.nK1jwl`: baseline-build-v2.log, baseline-focused/baseline-focused.ctrf.json, healthy-build.log, healthy-focused/healthy-focused.ctrf.json, candidate-build.log, candidate-focused/candidate-focused.ctrf.json, candidate-v2-build.log and candidate-v2-focused/candidate-v2-focused.ctrf.json. Native CodeCoverage 18.10.0 settings are byte-identical to the prior iteration's nine productive module settings, include auto-properties, disable default attribute exclusions, exclude test assemblies and enable only dynamic managed instrumentation. These are Core measurements, not full persistence/scheduling/transport coverage.

The checkpoint owns only five productive source/comment files, the handwritten test, eleven ledger bindings and this report. Normal atomic push is limited to the approved private feature branch and new annotated iteration tag, without force or unrelated refs. The exact branch, tag object and peeled commit must independently match after push. A fresh read-only GitHub API response confirms the approved repository is private and has push permission; no publication or external product-team authority is inferred from backup approval.

Next connected work remains receive-context constructor ownership, receive/Mediator cancellation logging parity and endpoint-related boundary invariants not proved here. Personally reading the actual SendEndpoint also identifies a source-backed parity candidate: its three `SendAsync<T>(object values, ...)` overloads directly await the transport result, unlike the explicit null-task diagnostic on materialized-message sends. This adjacent boundary is not corrected or behaviorally measured in iteration 135 and must not be described as closed. The TTL/retry/redelivery/outbox proof boundaries above remain explicit. Entire-source personal reading/comment/type/folder/API modernization, all API/parameter tests, full-fork coverage/CRAP and real provider acceptance remain open in the original unbounded goal. None is replaced by this package's narrower evidence.
