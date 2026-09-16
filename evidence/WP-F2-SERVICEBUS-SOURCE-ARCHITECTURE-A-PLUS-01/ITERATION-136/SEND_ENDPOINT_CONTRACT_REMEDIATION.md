# Iteration 136 — Send endpoint and initialized pipeline contracts

## Status and scope

Remediation and validation complete: 209 focused cases and all 4457 unfiltered Core cases pass; all 17 compiled causal counterchanges are killed and restored. The final evidence is reconciled below. The commit/tag/private-push checkpoint follows the final report review and must be independently verified at handoff. This report does not declare the original unbounded A+ goal complete.

The starting checkpoint is commit `767db191b1d13435f9c8fbc9d8a9c857e90730bb`, with its previously verified annotated iteration-135 tag and private normal atomic push. The owning development slice remains `WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`; its SHA256 is `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`. The governing agreement, glossary, decisions, current order, architecture entry point, findings, and slice were revalidated unchanged before this package's corrections.

The productive changes concern the real `SendEndpoint`, its connected `InitializerSendContextPipe`, and functional comments on `ISendTransport` and `ISendPipe`. No transport, persistence, scheduling, outbox, retry, redelivery, TTL, or durable-provider feature is removed. No SDK pinning, dependency change, compatibility alias, generated comment, suppression, or convenience directive is added.

## Structured diagnosis and corrections

The initial 136-case attempt compiled successfully with zero warnings and errors, but the native runner timed out. Its console snapshot reported 49 cases, 44 failures and five passes; its retained CTRF snapshot reported 50 cases, 45 failures and five passes. Neither incomplete snapshot is a completed baseline. The strict serializer double rejected `get_ContentType`, which the real `MessageSendContext.Serializer` setter reads before any configuration stage. Held-stage tests consequently never reached their gates. The fixture now supplies only this required getter, remains strict for every other serializer operation, and asserts the exact projected content-type instance at each configuration stage and at completion. Neither production behavior nor an oracle was relaxed to accommodate the fixture defect.

The corrected fixture baseline against unchanged productive code completed all 136 cases: 106 passed and 30 failed, no skipped, pending, or other cases. The failures were exactly: 20 metadata/token cases, one captured-context configuration case, three configuration-task null cases, three anonymous-values null transport-task cases, two null created-context results, and one null observer connection handle. Its native exit code was 2; compiler exit was 0.

The first correction passed all 136 cases. It forwards the exact context cancellation token to general and endpoint configuration, rejects null tasks with boundary-specific diagnostics, rejects null created contexts on fast and held paths, preserves completed provider task identity, and rejects a null observer handle. The completed-context check first snapshots terminal completion, avoiding a check-then-completion race that could otherwise bypass null-result validation. The pending path awaits the original provider task without an independent caller-cancellation race.

An internal readonly counterreview identified four Medium test gaps: independent observation of controlled original tasks during cleanup, valid pre-cancelled context creation, explicit contract types distinct from the materialized message type, and terminal proof that failed stages cause no later configuration or dispatch. All were addressed. Synchronous required-argument validation was additionally made uniform across the three values entrypoints through one shared asynchronous initialization/send implementation, without duplicating work or changing successful dispatch semantics.

Additional handwritten combinations cover immediate context-provider throw/fault/cancellation, plain typed and untyped pipes without a general configuration interface, null captured pipeline contexts, and cancellation while a caller-owned task-valued property remains pending. The declared-type test uses a real materialized message implementing a distinct interface and records the transport's actual generic contract; substituting `GetType()` cannot satisfy it.

The expanded 205-case baseline compiled after one analyzer-clean test correction and completed 184 passes and 21 failures. A dynamic expected type versus `typeof(T)` assertion triggered xUnit2000's constant heuristic; naming the actual generic type in a local preserves the conceptual expected/actual order without suppression. No native test ran after that failed compiler attempt. The 21 expanded baseline failures were exclusively held pipeline cases for initialized-values forms: three plain, nine typed-pipe, and nine untyped-pipe cases.

Reading the actual initializer revealed the additional productive defect: its header-decorated pipeline used `WaitAsync(context.CancellationToken)` after starting the downstream endpoint configuration. Caller cancellation detached that owned operation. The corrected downstream await retains the actual configuration task and its original success, failure, or provider cancellation; a null inner task now has its own explicit diagnostic. Cancellation of waits for caller-owned input values remains intact and is separately proved. The unchanged property/header initializer and multi-input traversal cancellation boundaries are not certified by this downstream correction.

The final focused candidate contains 23 methods and 209 cases, all passing with compiler/native exit 0 and no skips, warnings, pending, or other outcomes. Its compiler reports zero warnings/errors and 24.30 seconds; the CTRF interval is 388 ms. CTRF SHA256 is `2455a6c560c1fbef305f97d036936f5fa770cb07627a33b755472b1bd383d8f0`.

## Requirement-to-test evidence

All rows are exact methods in `ViciOne.ServiceBus.Tests.Transports.SendEndpointContractTests`, assembly `ViciOne.ServiceBus.Tests`. All 2976 current requirements rows retain the 2953 parent rows and add 23 exact attribute/method bindings. The final passing focused run supplies these case counts; no source-name pairing or coverage inference substitutes for behavioral assertions.

| Contract or interaction | Exact method | Cases |
| --- | --- | ---: |
| Null transport task across every send form | `EverySendForm_RejectsNullTransportTaskAsync` | 10 |
| Valid pre-cancelled sends have no transport/configuration effects | `EverySendForm_PreCancellationNeverInvokesTransportAsync` | 10 |
| Immediate transport throw/fault/provider cancellation retain original identity/token | `EverySendForm_PreservesImmediateTransportFailureOrCancellationAsync` | 30 |
| Started transport ownership survives caller cancellation across every form | `EverySendForm_OwnsHeldTransportAndItsOriginalOutcomeAsync` | 30 |
| Exact serialization/address/token/order with existing or absent metadata | `EverySendForm_AppliesExactMetadataTokensAndPipelineOrderAsync` | 20 |
| Actual task-valued properties and convention headers initialize before dispatch | `PendingValues_AreReallyInitializedBeforeTransportAsync` | 3 |
| Every required argument has synchronous precedence over cancellation | `RequiredArguments_WinBeforeCancellationAcrossEveryFormAsync` | 2 |
| Null context task, completed null result, and held null result | `ContextCreation_RejectsNullTaskAndImmediateOrHeldNullResultAsync` | 3 |
| Held context creation owns original success/failure/provider cancellation | `ContextCreation_OwnsHeldProviderAndItsOriginalOutcomeAsync` | 3 |
| Completed context creation preserves exact task and context | `ContextCreation_FastPathPreservesExactProviderTaskAndContextAsync` | 1 |
| Actual captured context pipeline configures without physical dispatch | `ContextCreation_CapturedPipelineConfiguresWithoutDispatchAsync` | 1 |
| Every configuration stage has an exact null-task diagnostic and no later effects | `Pipeline_RejectsNullTaskWithExactStageDiagnosticAsync` | 3 |
| Every reachable stage in every send form retains original outcome and terminal stage boundary | `Pipeline_OwnsEachHeldStageAndItsOriginalOutcomeAsync` | 66 |
| All constructor dependencies are validated without dispatching or disposing the transport | `Constructor_ValidatesEveryDependencyWithoutDispatchAsync` | 7 |
| Observer validation, exact handle/failure, and null-handle diagnosis | `ObserverConnection_PreservesHandleAndOriginalFailureOrNullDiagnosticAsync` | 3 |
| Captured probe validates and forwards the exact probe without sending | `PipelineProbe_ValidatesAndForwardsTheExactProbeContextAsync` | 1 |
| Valid pre-cancelled context creation has no provider effects | `ContextCreation_PreCancellationNeverInvokesTransportAsync` | 1 |
| Explicit interface contract differs from runtime type in both explicit-type forms | `ExplicitRuntimeContract_PreservesDeclaredTypeAndMessageIdentityAsync` | 2 |
| Immediate creation throw/fault/provider cancellation preserve exact outcomes and completed tasks | `ContextCreation_PreservesImmediateProviderFailureOrCancellationAsync` | 3 |
| Null captured context never configures or dispatches | `Pipeline_RejectsNullContextBeforeConfigurationAsync` | 1 |
| Plain additional pipes follow endpoint configuration without a general stage | `PlainAdditionalPipe_AppliesAfterEndpointWithoutGeneralConfigurationAsync` | 2 |
| Input cancellation leaves the caller-owned value pending without dispatch | `PendingValues_CancellationLeavesCallerOwnedValueUnsettledWithoutDispatchAsync` | 3 |
| Real initialized headers precede inner null diagnosis or owned original completion | `InitializedHeaders_RejectNullInnerTaskAndOwnItsOriginalOutcomeAsync` | 4 |

Controlled tasks are released in `finally`; original provider tasks are observed independently of roots and captured transport tasks. No sleeps, skipped cases, public dummy fallback, or reconstructed missing source is used. The fixture creates real message send contexts and invokes the actual dispatcher, initializer cache, property/header conventions, endpoint pipeline, and metadata setters. Strict context/serialization proxies and scripted transport operations are controlled test boundaries, not live provider acceptance.

## Personal reading and structural review

The connected productive inventory extends the 29-file/4406-line inventory documented in iteration 135 by nine additional fully personally read productive files. Current total: 38 unique files and 5192 lines. Sorted path/line-count/content manifest SHA256: `62d2243d504262b7ce95f884168c5f4201e61a623970b4c6384007cef2fd4292`. Canonical rows are UTF-8 path, TAB, current line count, TAB, SHA256, LF. Hashing validates current bytes; it is not credited as reading. The main personally read each new productive file in full, not through the counterreview's summary.

| Additional productive file | Current lines |
| --- | ---: |
| `src/ViciOne.ServiceBus/Initializers/MessageInitializerCache.cs` | 173 |
| `src/ViciOne.ServiceBus/Initializers/MessageInitializer.cs` | 299 |
| `src/ViciOne.ServiceBus.Abstractions/Internals/Dispatching/SendEndpointDispatcher.cs` | 143 |
| `src/ViciOne.ServiceBus.Abstractions/Advanced/Initializers/InitializedMessage.cs` | 46 |
| `src/ViciOne.ServiceBus.Abstractions/Transports/ISendTransport.cs` | 30 |
| `src/ViciOne.ServiceBus.Abstractions/Transports/ISendPipe.cs` | 8 |
| `src/ViciOne.ServiceBus.Abstractions/ISendEndpoint.cs` | 26 |
| `src/ViciOne.ServiceBus/Transports/Sending/SendEndpointFactory.cs` | 12 |
| `src/ViciOne.ServiceBus.Abstractions/Middleware/IPipe.cs` | 15 |

Before test edits, all 625 previously personally read owning inputs were revalidated against the parent commit with exact working Git blob equality. The new handwritten test and its amendments were personally read and reviewed against actual source behavior. The current owning set has 626 inputs: 562 Core plus 64 support/build inputs. Sorted path/content manifest SHA256: `146c18d1f6d980d8ea5170ca9956b79080607693672da1dec8d4654c2cfec312`. This is a connected inventory, not proof that the entire fork has already been read or reached A+.

Comments were manually written after understanding the relevant productive code. They describe message contracts, transport acceptance rather than consumption, configured non-null contexts, pipeline order, endpoint operation ownership, and diagnostic probing. No authoring generator or script was used. Top-level filenames match their types; endpoint-local and initializer-local pipeline types remain encapsulated in their owning implementation. No public method, feature, or declared-type route is removed; asynchronous operation names retain `Async`, including shared implementation helpers. Whole-fork bidirectional naming and directory review remain separate open requirements.

## Compiled causal counterchanges

All 17 distinct, literally hand-authored counterchanges compiled successfully with warnings as errors and produced intentional native exit 2. Each selection uses `--filter-method ViciOne.ServiceBus.Tests.Transports.SendEndpointContractTests.<exact-method>` and its declared minimum count. Native reports contain no skipped, pending, or other outcomes. All six frozen productive/test/ledger hashes were verified before the batches and after every literal restoration. No test, threshold, skip policy, diagnostic expectation, or production baseline was relaxed. Across these runs, 179 selected cases produced 98 passes and 81 intentional failures. This is a bounded causal evidence set, not a whole-fork mutation score.

| ID | Compiled counterchange | Exact selected method | Total / pass / fail |
| --- | --- | --- | ---: |
| M01 | Remove initialized-values null transport-task diagnosis | `EverySendForm_RejectsNullTransportTaskAsync` | 10 / 7 / 3 |
| M02 | Remove context-creation null-task diagnosis | `ContextCreation_RejectsNullTaskAndImmediateOrHeldNullResultAsync` | 3 / 2 / 1 |
| M03 | Remove completed null-context result diagnosis | `ContextCreation_RejectsNullTaskAndImmediateOrHeldNullResultAsync` | 3 / 2 / 1 |
| M04 | Remove held null-context result diagnosis | `ContextCreation_RejectsNullTaskAndImmediateOrHeldNullResultAsync` | 3 / 2 / 1 |
| M05 | Remove general-stage null-task diagnosis | `Pipeline_RejectsNullTaskWithExactStageDiagnosticAsync` | 3 / 2 / 1 |
| M06 | Remove endpoint-stage null-task diagnosis | `Pipeline_RejectsNullTaskWithExactStageDiagnosticAsync` | 3 / 2 / 1 |
| M07 | Remove additional-stage null-task diagnosis | `Pipeline_RejectsNullTaskWithExactStageDiagnosticAsync` | 3 / 2 / 1 |
| M08 | Run additional configuration before endpoint configuration | `EverySendForm_AppliesExactMetadataTokensAndPipelineOrderAsync` | 20 / 8 / 12 |
| M09 | Omit general and endpoint context cancellation tokens | `EverySendForm_AppliesExactMetadataTokensAndPipelineOrderAsync` | 20 / 0 / 20 |
| M10 | Detach initialized-values transport with caller `WaitAsync` | `EverySendForm_OwnsHeldTransportAndItsOriginalOutcomeAsync` | 30 / 21 / 9 |
| M11 | Remove null observer connection-handle diagnosis | `ObserverConnection_PreservesHandleAndOriginalFailureOrNullDiagnosticAsync` | 3 / 2 / 1 |
| M12 | Detach the real initializer's started inner endpoint pipeline | `Pipeline_OwnsEachHeldStageAndItsOriginalOutcomeAsync` | 66 / 45 / 21 |
| M13 | Detach held context creation with caller `WaitAsync` | `ContextCreation_OwnsHeldProviderAndItsOriginalOutcomeAsync` | 3 / 0 / 3 |
| M14 | Substitute runtime type for the explicitly declared contract | `ExplicitRuntimeContract_PreservesDeclaredTypeAndMessageIdentityAsync` | 2 / 0 / 2 |
| M15 | Remove valid context-creation pre-cancellation check | `ContextCreation_PreCancellationNeverInvokesTransportAsync` | 1 / 0 / 1 |
| M16 | Remove real initialized inner-pipe null-task diagnosis | `InitializedHeaders_RejectNullInnerTaskAndOwnItsOriginalOutcomeAsync` | 4 / 3 / 1 |
| M17 | Defer required values validation into an asynchronous entrypoint | `RequiredArguments_WinBeforeCancellationAcrossEveryFormAsync` | 2 / 0 / 2 |

Null-task counterchanges fail only their exact task boundary with `NullReferenceException` instead of the required `InvalidOperationException`; null-result and handle counterchanges fail because no exception is thrown. The order counterchange fails the twelve additional-pipe cases at the expected endpoint/additional boundary. Token omission fails all twenty exact-token metadata cases. Detachment counterchanges fail only their corresponding owned-operation cases with premature root completion. Declared-type substitution fails both explicit contract forms with the actual wrong generic contract. Removing pre-cancellation reaches the poisoned provider instead of producing the exact caller-token cancellation. The asynchronous-validation counterchange fails both precedence cases because the required exception is no longer synchronous. Complete witnesses remain in each native CTRF, not just the summarized counts.

## Final Core validation and evidence

The fresh final Release compiler exits 0 with zero warnings/errors in 61.43 seconds. The native unfiltered runner exits 0: 4457 total, 4457 passed, zero failed/skipped/pending/other. Native platform duration is 21.220 seconds; the CTRF interval is 17.981 seconds. The complete 4248-case display-name multiset from iteration 135 is retained, including repeated names, and the new cohort exactly equals the passing focused 209-case multiset across 23 methods. Canonical retained name/count multiset SHA256 is `1fd74fad08ae84f268313f5fedd862e42925a337d8ccdcfbf12134169f134aed`.

The same-run native Cobertura XML parses and contains exactly the nine configured productive Core modules, with no test assembly. Weighted root rates independently agree with 50653/64603 covered/valid lines, 78.4065755460%, and 17513/24340 covered/valid branches, 71.9515201315%. This is not full-fork coverage or all-API/parameter completeness; no CRAP score is calculated.

| Native productive Core module | Line % | Branch % |
| --- | ---: | ---: |
| `ViciOne.ServiceBus` | 80.37 | 74.23 |
| `ViciOne.ServiceBus.Abstractions` | 59.42 | 53.70 |
| `ViciOne.ServiceBus.Courier` | 89.10 | 75.44 |
| `ViciOne.ServiceBus.Futures` | 90.57 | 85.48 |
| `ViciOne.ServiceBus.Initializers` | 100.00 | 100.00 |
| `ViciOne.ServiceBus.JobService` | 95.63 | 89.75 |
| `ViciOne.ServiceBus.Mediator` | 91.07 | 78.66 |
| `ViciOne.ServiceBus.Sagas` | 64.23 | 57.84 |
| `ViciOne.ServiceBus.Testing` | 94.38 | 77.98 |

Collector exclusions are unchanged: test assemblies excluded, no attribute-based productive exclusion, auto-properties retained. Configuration SHA256 matches iteration 135: `c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13`. The collector's root complexity is not relabeled as a computed CRAP score.

| Final local artifact | SHA256 |
| --- | --- |
| `final-build.log` | `59c192ea9c5c6690af4e8294c35ae9579127d0b3e6f90f910b5a9713b0b31293` |
| `final-core-native.log` | `7b74df7e52473767bae5fc94be9c983e43401690b9c6ba3b2d010a19f77348a2` |
| `final-core/final-core.ctrf.json` | `a144ac8071e57608d87da4a36c2d57f78b31df349085eb43735ef8049a929d98` |
| `final-core/final-core.cobertura.xml` | `d3b4a87f55c94563cbacb986c8cf336aa306397105c8575e66b8d38e716bbaef` |
| Owning test output `ViciOne.ServiceBus.Tests.dll` | `b837bf352cbc5368782ab2ec81592f5000f307d7803bd120700f738efc81e66d` |
| Owning test output productive `ViciOne.ServiceBus.dll` | `6a4c15978c00d297dceab3d928e72b823613ae8a7974e35fa9288a6c6e852d69` |

The final unique binlog is `final-build-20260916-012355--80755--t8tZNU.binlog`. Every M01–M17 compiler's unique binlog and successful zero-warning/error summary is independently present. Commands executed with output redirected to their dedicated logs and build-to-native `&&`:

```text
TMPDIR='/private/tmp/vsb-iteration136-send-endpoint.ScTR9s' dotnet build tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -warnaserror '/bl:/private/tmp/vsb-iteration136-send-endpoint.ScTR9s/final-build-{}.binlog'
TMPDIR='/private/tmp/vsb-iteration136-send-endpoint.ScTR9s' dotnet exec artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests.dll --results-directory '/private/tmp/vsb-iteration136-send-endpoint.ScTR9s/final-core' --minimum-expected-tests 4457 --zero-tests-policy strict --fail-skips on --fail-warns on --progress off --timeout 5m --report-xunit-ctrf --report-xunit-ctrf-filename final-core.ctrf.json --coverage --coverage-output-format cobertura --coverage-output '/private/tmp/vsb-iteration136-send-endpoint.ScTR9s/final-core/final-core.cobertura.xml' --coverage-settings '/private/tmp/vsb-iteration136-send-endpoint.ScTR9s/core-productive-coverage.config'
```

Raw artifacts remain outside productive source and tests under `/private/tmp/vsb-iteration136-send-endpoint.ScTR9s`. Each compiler uses isolated `TMPDIR`, the established approved sandbox IPC exception, Release/no-restore/disabled build servers/single-node compilation, shared compilation disabled, warnings as errors, and a unique binary log. Native execution follows a successful compiler through `&&`, with strict zero/minimum/skips/warnings policies and bounded runner timeout. The SDK is 10.0.302; MTP uses the existing xUnit v3 standalone executable, not VSTest flags or a separator.

Frozen candidate SHA256 values:

| Productive or owning file | SHA256 |
| --- | --- |
| `SendEndpoint.cs` | `f94ac75798b406c4bf15da3df7e6a93679eec6ca6f765025270a28b8f961d2a6` |
| `MessageInitializer.cs` | `a120e845df0d1e3f18fe18e4e4d99396e40b318f4cd26a1468ef1d9d9452e821` |
| `ISendTransport.cs` | `aa4e415328882dae07adbbf7e9d0029ba12759919412dc522f7aa32d5734ac88` |
| `ISendPipe.cs` | `9dfa9a231f0c1d4d95f8043d7e949f75df4dc56d2c30dfc77e5c872c69cf14d5` |
| `SendEndpointContractTests.cs` | `25ba94bdb723771a7900d2d6c4089c9f9a42170abe67c95ffa95da8ae93a94a9` |
| `CoreRequirements.json` | `aae9a62741db97f63f89c32a4d87ac3b89724b74ba101abc910913f1f622974c` |

## Review attribution and remaining work

The delegated counterreview is an explicitly authorized internal Sol agent, not an independent external product team or external red-team acceptance. Its bounded findings improve the oracles; the main owns productive reading, literal code/comment/test changes, builds, native tests, causal counterchanges, and Git reconciliation. The agent fully re-read the stable 1131-line test, all 23 methods/209 declared cases, the complete 296-line endpoint and 299-line initializer, and the connected transport contracts. Before/after hashes match the frozen candidate. It independently verified the unchanged 2953-row ledger prefix and 23 exact additive bindings, the race-safe completion/null guard, the four Medium corrections, synchronous entrypoints, temporal stage boundaries, and independent original-task cleanup, with no remaining concrete bounded finding. The agent did not run tests or mutations. Its final full-report/raw-evidence reconciliation completed with no remaining concrete findings after correcting one Low constructor-evidence wording issue and explicitly distinguishing the incomplete initial console and CTRF snapshots. It reconciled the exact retained/new case multisets, causal witnesses, coverage scope and fractions, durations, and artifact/input hashes. The successful combined-command terminal exit is main-observed evidence, not an agent execution or an inference from logs.

Next connected review must assess initializer callback/header/multi-input ownership and cancellation with actual producer boundaries, then remaining receive-context constructor ownership and receive/Mediator cancellation logging parity. Caller-owned task-valued inputs must not be confused with started component-owned configuration. None of the remaining initializer paths is implicitly certified by the downstream endpoint-pipe correction.

The original goal still requires whole-source personal reading/comment/type/folder/API modernization, all API and new-parameter tests, whole-fork coverage/CRAP, real transport/persistence/scheduler/durable-provider acceptance, feature-retention evidence, and iterative multidimensional follow-up. This package does not shrink or replace those requirements. Checkpoint protocol: commit exactly this package's four productive files, handwritten test, additive requirements ledger, and report; create a new annotated iteration-136 tag without overwriting a tag; perform the approved normal atomic private push; independently verify exactly the branch, annotated tag object, and peeled commit refs. Successful remote completion must be established from those actual operations at handoff, not inferred by this report before its own commit.
