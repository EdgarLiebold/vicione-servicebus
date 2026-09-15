# Iteration 119 — in-memory saga ownership and boundary contracts

## Scope and authority

This is a bounded checkpoint within the original A+ Greenfield API/code-architecture
goal, not completion of iteration 119, whole-product acceptance or independent
external Red Team acceptance. The original goal remains active, with no feature
removal or source-reading obligation waived.

PO order: `PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`.
Development-slice SHA256:
`5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.
Starting commit: `40ff13c45018fb0a9436d34cd0618024f2000620`.
Its branch and annotated context-contract checkpoint were verified on origin before
this packet. Protected `review/` and `TestResults/` content is not accessed or changed.

The lead personally reads and understands all nine changed source files before
manually writing their comments. No source, test, comment, namespace or folder
generator is used. Independent SDK projects are not nested in the Core compilation
tree: `src/ViciOne.ServiceBus` owns the Core assembly, while Abstractions, Sagas,
Initializers and integration-family projects retain their own compile boundaries.
Same namespace is not the same assembly. No whole-project move is made here.

## Product corrections and preserved semantics

- Removed-state loads no longer release a saga semaphore they did not acquire.
  Load-only contexts return referenced state without owning its saga lease.
- Consume-context disposal transfers and releases its acquired saga exactly once,
  including concurrent/repeated disposal and a subsequent legitimate owner.
- Repository disposal requests nonblocking once-only initial dictionary release.
  Active operations retain that ownership until the final initial-lease user exits.
  Creation success/failure and deletion both participate; disposal is not a
  synchronous wait or a new terminal prohibition on later context operations.
- Delete/Discard use an already-owned initial dictionary lease instead of waiting
  on themselves. Removal is guarded by exact retained-state reference identity;
  a stale context cannot delete a replacement with the same correlation identifier.
- Acquisition recovery returns null for a retained, permanently invalidated saga
  rather than repeatedly retrying it until cancellation.
- Required state/context/factory/callback inputs and valid consume-context modes
  receive exact validation before acquisition, mutation or callback effects.
  A callback returning a null task has an explicit diagnostic; a valid task may
  return a null load result. Pre-cancelled callbacks are never invoked.
- Real Add/Insert/Load, occupied Insert refusal, exact state identity, mutable-state
  acknowledgement, cancellation-token selection, successful removal, missing-state
  diagnostics and callback exception identity remain behaviorally tested.
- In-memory Save/Update/Undo do not clone or restore referenced mutable state.
  Comments describe acknowledgement/cancellation, not fictional transactions.
  Factory comments distinguish provider-specific acquisition/persistence ownership.

No public member signature is removed or renamed. One private callback helper is
renamed from the redundant `ExecuteAsyncMethodAsync` to `ExecuteCallbackAsync`.
This is not a claim that every public parameter or every source file is now A+.

## Personal-read and accepted byte manifest

Paths are relative to the repository. All nine source rows are complete personal
reads with manual current-functional comment review, not heuristic source/test
pairings. Additional personally read query/index/pipeline source is research for
the still-open connected packet and is not counted as corrected architecture here.

| Source file | Accepted SHA256 |
|---|---|
| `src/ViciOne.ServiceBus.Sagas/Context/DefaultSagaConsumeContext.cs` | `182112589a982b317499fe02c261fb4bb6a0ff6284442f9ed227b692245b3d3a` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ISagaConsumeContextFactory.cs` | `402117a12e9d0fdb64a93197fedf74cf384b78e43d3b59baf93250f33d226b45` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaConsumeContextFactory.cs` | `d25c48f7feb4fec798b54c5ae0468ffec21937d57981c6b951ab43492e601dcf` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/InMemorySagaConsumeContext.cs` | `83e69a503f25d691a139acded6e1d5e07af35d5628490b899eb1e9a33f2924ca` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/InMemorySagaConsumeContextFactory.cs` | `46f536d9847b046e49397aa883ba2be71c775591f6a86f0775edf8a862131aa6` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/InMemorySagaRepositoryContext.cs` | `6ac15f0ee6852ff19ff83b4b2ed2302bf75d769cccd620eeaeb79b03090776e8` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/InMemorySagaRepositoryContextFactory.cs` | `b9f58763cbb0ecc4cc4625ea62ff066ea00be7348a86d937f7074f90ba37c81f` |
| `src/ViciOne.ServiceBus.Sagas/Saga/InMemoryRepository/SagaInstance.cs` | `c3d0ad9ba58ffd5c7f0069aa3dce5fc06f879fb1cb3c48ba50a2bec90a561032` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/InMemorySagaRepository.cs` | `e29cee030f767682c5e31a31b0ae3cfd3fa4a98dfc147505878e9ffe9e2a8309` |

| New test file / updated catalogue | Accepted SHA256 |
|---|---|
| `tests/ViciOne.ServiceBus.Tests/Saga/InMemoryRepository/InMemorySagaRepositoryContextTests.cs` | `038452ad5421798068064a02e4f1b49bebc34baf6921a2be464b9a9ee2bd9fed` |
| `tests/ViciOne.ServiceBus.Tests/Saga/InMemoryRepository/InMemorySagaConsumeContextTests.cs` | `0283c4a3aaecfd97be39d009706fcaf556c36c1117ecd4379619bf9347562c54` |
| `tests/ViciOne.ServiceBus.Tests/Saga/InMemoryRepository/InMemorySagaRepositoryContextFactoryTests.cs` | `a0d10a9c4000cf7dd1041e25546f9ebf870b76eb90aa363efe1a41314d1b587f` |
| `tests/ViciOne.ServiceBus.Tests/Saga/SagaConsumeContextFactoryTests.cs` | `d946d0ae92cc9d76a8a9ffdee4cc3bb0cfc4fe9b360e04eadedbee1ccf1f8b8c` |
| `tests/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `7024a4ea7217c2f100ba44ae161accb7742c1891c10c0693fbb3abafa59b1d08` |

## Exact requirement-to-test map and assertion review

All 29 methods have `REQ-VSB-SAGA-REPOSITORY-CONTEXT` requirement metadata.
Twenty-nine exact tuples are added manually; the catalogue contains 2,864 unique
tuples. `CoreRequirements_MatchCompiledRequirementMetadata` verifies the embedded
catalogue against actual compiled xUnit metadata in the complete Core execution.
Namespaces: `ViciOne.ServiceBus.Tests.Saga.InMemoryRepository` for the three
in-memory classes; `ViciOne.ServiceBus.Tests.Saga` for the generic factory class.

| Owning class | Exact method | Cases | Direct assertions |
|---|---|---:|---:|
| `InMemorySagaRepositoryContextTests` | `RemovedSaga_LoadOnlyReturnsNullWithoutReleasingAnotherOwnersLeaseAsync` | 2 | 4 |
| same | `RemovedSaga_MessageLoadReturnsNullWithoutReleasingAnotherOwnersLeaseAsync` | 4 | 4 |
| same | `RepeatedAndConcurrentDispose_ReleaseOnlyTheRepositorysDictionaryLeaseAsync` | 2 | 3 |
| same | `AddInsertAndLoad_PreserveStateIdentityAndRefuseAnExistingInsertAsync` | 2 | 9 |
| same | `ExistingInsert_RetainsTheInitialDictionaryLeaseUntilDisposalAsync` | 2 | 3 |
| same | `RequiredOperationInput_IsRejectedWithoutChangingTheRepositoryAsync` | 7 | 5 |
| same | `PersistenceCancellation_UsesTheExplicitTokenOrFallsBackToTheConsumeTokenAsync` | 10 | 4 |
| same | `Acknowledgement_UsesAnExplicitLiveTokenAndKeepsTheSameMutableStateAsync` | 3 | 3 |
| same | `DeleteAndDiscard_RemoveTheRetainedStateAndReleaseTheDictionaryAsync` | 2 | 3 |
| same | `StaleContext_CannotDeleteAReplacementWithTheSameCorrelationIdAsync` | 2 | 4 |
| same | `MissingDelete_ReportsTheExactCorrelationIdAndReleasesTheDictionaryAsync` | 1 | 2 |
| same | `DeleteAndDiscard_UseTheInitialDictionaryLeaseAndReleaseItOnSuccessOrFailureAsync` | 4 | 7 |
| same | `InvalidationDuringAcquisition_ReturnsNullWithoutRetryingTheRetainedRemovedSagaAsync` | 1 | 5 |
| same | `DisposeDuringCreation_KeepsTheDictionaryLeaseUntilTheFactoryCompletesAsync` | 2 | 7 |
| same | `DisposeDuringDelete_KeepsTheDictionaryLeaseUntilRemovalCompletesAsync` | 1 | 7 |
| same | `ConcurrentInitialOperations_KeepTheDictionaryLeaseUntilTheLastFactoryCompletesAsync` | 1 | 8 |
| same | `FaultedInitialCreation_ReleasesTheDeferredLeaseAndPreservesTheExactFaultAsync` | 2 | 7 |
| `InMemorySagaConsumeContextTests` | `RepeatedAndConcurrentDispose_ReleaseOnlyTheConsumeContextsSagaLeaseAsync` | 2 | 5 |
| same | `RequiredConstructionInput_IsRejectedWithItsExactParameterName` | 5 | 3 |
| same | `Completion_CancelledExplicitTokenDoesNotCompleteButALaterSuccessfulCallDoesAsync` | 1 | 5 |
| `InMemorySagaRepositoryContextFactoryTests` | `RequiredConstructorDependency_IsRejectedWithItsExactParameterName` | 6 | 3 |
| same | `Execute_RejectsNullCallbacksAndNullTasksForBothCapabilitiesAsync` | 2 | 5 |
| same | `Execute_PreCancellationDoesNotInvokeTheCallbackAndPreservesTheExactTokenAsync` | 2 | 3 |
| same | `Execute_PreservesTheExactResultTokenAndCallbackFaultAsync` | 2 | 4 |
| same | `Execute_SynchronousCallbackFaultRetainsItsExactIdentityAsync` | 2 | 2 |
| same | `Execute_NullLoadResultIsAllowedWhenItsTaskIsValidAsync` | 1 | 2 |
| `SagaConsumeContextFactoryTests` | `RequiredFactoryInput_IsRejectedBeforeAcquisitionOrMutationAsync` | 6 | 3 |
| same | `InvalidMode_IsRejectedBeforeAcquisitionOrMutationAsync` | 2 | 3 |
| same | `DefaultFactory_EachValidModeWrapsTheSameStateWithoutMutatingTheRepositoryAsync` | 3 | 4 |
| Total | 29 methods | 82 | 127 |

The lead manually reviews every method and its helpers. A read-only assertion
inventory assists, but does not replace, this review: 4.3793 direct assertion sites
per method; shared-helper assertions are excluded. Six primary mechanisms occur:
equality/reference (28 methods), boolean (15), null (10), exception (17), runtime
type (2), string-content containment (1). Category histogram: one method has one
primary category, 14 have two, 12 have three and two have four; average 2.5172.
The single-category method compares actual identity, correlation, token and
repository count across all valid modes; it is not trivial self-comparison.

There are zero assertion-free, trivial-only or tautological methods, zero skips
and no sleeps/delays, wall-clock polling, unawaited actual operations or suppression
directives in the changed tests. Real negative/fault cases assert narrow exceptions,
exact parameters/tokens/diagnostics/identity and no mutation. State checks include
before/after registration, invalidation, counts and exact retained references.
Two runtime-type checks validate the returned context boundary; this packet does
not rely on reflection-only tests for product correctness. Gates, not timings,
establish operation ordering. Five-second linked cancellation bounds cancel the
actual dictionary/saga operation; every started operation is awaited/drained and
only successfully acquired foreign leases are released.

## Causal history and internal adversarial review

Full native logs are retained outside protected trees at
`/private/tmp/vsb-iteration119-saga-pairing.iEltCW`.
The parse-only Roslyn pairing result is 360 Saga source / 545 Core test files,
92 paired / 268 unpaired. This heuristic is not coverage or behavioral proof;
the initial analyzer process handle was not retained, so no observed analyzer
exit-zero claim is made solely from the later valid JSON.

Initial strict test wiring fails with 46, then two compile diagnostics. Explicit
tokens and correct nullable task conversions repair them without suppression.
The first 68-case run has 62 DispatchProxy fixture type-load failures because the
new closed message contracts were private; all four nested test messages are made
public. That mixed fixture run is not accepted as causal product red. The clean
repeat has 32 pass / 36 actual behavioral failures. Subsequent bounded additions
establish causal self-wait, invalidation-retry and active-disposal ownership failures
before their corresponding corrections. The final source is validated by 82 cases.

The separate internal `gpt-5.6-sol` read-only reviewer checks frozen source/tests
and entry/exit hashes. It finds a major active-operation/Dispose ownership defect
and exceptional-cleanup warnings. These are repaired with deferred reference-counted
release, actual acquisition cancellation, unconditional nested contender cleanup
and `Task.WhenAll` draining of both creation operations. Three later cleanup-only
reviews confirm the final delta and explicitly release their freezes. The final
reviewed test hash `8af11c...` replaces intermediate `8385...`, `cd0e...` hashes; accepted source
hashes remain unchanged. No remaining concrete finding is reported within this
bounded packet. This is internal engineering review, not external acceptance.

Mutation testing itself discovers two cleanup-cancellation masking paths. They are
corrected manually and reviewed; the functional exact-token and exact-fault assertions
are retained. `SagaInstance.MarkInUseAsync` uses a linked caller/removal token, so
cleanup requires its own caller cancellation to be requested rather than assuming
its exception carries the original token. Production token-normalization remains
open for the connected cancellation packet. First masked failures do not count
as intended causal kills. One null-task test observation overlaps an unobserved
build completion and is excluded; its repeat starts after observed build termination.

Scoped Unit whitespace verification detects ten lines of switch-expression
indentation in the new test helper. These are corrected manually after both format
hosts terminate. Reversing only those ten indentation changes in memory reproduces
the exact reviewed `8af11c...` SHA256; the final `038452...` binding above has no
semantic delta. Strict build/full Core/fresh coverage are nonetheless repeated
against these final test bytes. Product source hashes and mutation meanings do
not change. Product whitespace verification exits 0 with the pre-existing workspace
load warning; final Unit verification follows the correction.

## Individually compiled mutations

Each counterchange is applied manually, strictly compiled through the owning Core
test project, executed against the exact covering test(s), and restored manually
only after its native host terminates. Every accepted restore is checked against
the complete source SHA256 manifest above. Static hypotheses are not called kills.

| Mutation | Manual product counterchange | Exact covering method(s) | Accepted native failures |
|---|---|---|---:|
| M1 | Replace atomic consume-context lease transfer with unconditional Release | `RepeatedAndConcurrentDispose_ReleaseOnlyTheConsumeContextsSagaLeaseAsync` | 2/8 |
| M2 | Reintroduce unowned saga Release in load-only removed-state handling | `RemovedSaga_LoadOnlyReturnsNullWithoutReleasingAnotherOwnersLeaseAsync` | 2/2 |
| M3 | Release initial dictionary when active users <= 1 instead of == 0 | `ConcurrentInitialOperations_KeepTheDictionaryLeaseUntilTheLastFactoryCompletesAsync` | 1/1; direct blocked-owner assertion in corrected rerun |
| M4 | Remove replacement reference-identity guard | `StaleContext_CannotDeleteAReplacementWithTheSameCorrelationIdAsync` | 2/2 |
| M5 | Remove Execute pre-cancellation check | `Execute_PreCancellationDoesNotInvokeTheCallbackAndPreservesTheExactTokenAsync` | 2/2 |
| M6 | Remove Save context null guard; Update forwards to Save | `RequiredOperationInput_IsRejectedWithoutChangingTheRepositoryAsync` | 2/7 |
| M7 | Permit callback to return a null task | `Execute_RejectsNullCallbacksAndNullTasksForBothCapabilitiesAsync` | 2/2 in post-build repeat |
| M8 | Release initial dictionary without checking active users | Active creation/deletion/multiple-user/fault-creation methods | 6/48 in corrected repeat; all six direct blocked-owner assertions |
| M9 | Ignore invalidation after removed-instance acquisition recovery | `InvalidationDuringAcquisition_ReturnsNullWithoutRetryingTheRetainedRemovedSagaAsync` | 1/1; retry continues until actual operation cancellation instead of returning null |

All nine selected, individually compiled mutations are killed; none survives and
none fails to compile. These nine targeted kills are not a whole-product mutation
score. Each accepted mutant execution exits 2; every strict mutation build exits 0
with zero warnings/errors. The final canonical product bytes are restored before
the closing build, focused execution and complete Core run.

## Closing validation

Final post-mutation build exits 0, zero warnings/errors; focused execution is
82/82, no failures/skips. After the manual indentation correction, the final strict
build again exits 0, zero warnings/errors. Complete Core native execution against
these final bytes is 3,910/3,910, zero failures/skips; its CTRF accounts for all 82
packet cases and successful compiled requirement-metadata verification. Earlier
Core 3,904 and focused 79/80 executions are not substituted for this closing run.

Fresh final Core source-only coverage: 49,423/60,987 lines (81.0386%),
16,936/23,148 branches (73.1640%). The explicit native profile includes source
under `src`, no source exclusions, no attribute exclusions, and auto-properties;
test assemblies are not counted. It measures the loaded Core assembly graph,
not every provider, transport or whole product. Product scoped whitespace verification
exits 0; final corrected Unit verification also exits 0. Both retain the known
workspace-load warning and report no final whitespace errors. All owned build,
test and format processes terminate before Git capture. Raw artifact hashes are recorded in
[SAGA_OWNERSHIP_ARTIFACT_HASHES.md](SAGA_OWNERSHIP_ARTIFACT_HASHES.md).

The separate fresh Abstractions host has 749 passed / zero failed/skipped and
native source-only coverage: 5,332/8,310 lines (64.1637%), 1,862/2,996 branches
(62.1495%). It is an assembly-graph measurement, not whole-product coverage.
Overlapping Core and Abstractions coverage totals must not be summed. No CRAP
analysis or real cloud-provider acceptance is inferred from these numbers.

Reproducible commands use SDK 10.0.302, Release, the existing strict build settings
and native xUnit/Microsoft Testing Platform v2 executable:

```text
dotnet build tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --disable-build-servers -m:1 -v:minimal
artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests --results-directory /private/tmp/vsb-iteration119-saga-pairing.iEltCW/post-format-core-final-results --report-xunit-ctrf --coverage --coverage-settings tools/ci/coverage.settings.xml --coverage-output post-format-core-final.cobertura.xml --coverage-output-format cobertura
```

Exact mutation selection uses the native `--filter-method` fully-qualified owning
type and exact method from the table above; M1/M8 instead run their full owning
class using `--filter-class`. No legacy VSTest filter syntax is mixed into the
native command. Build/test/format processes run with the necessary local socket
permission, not by weakening analyzers or replacing product behavior. The known
Sandbox Roslyn/MTP IPC limitation is retained as an operating lesson. All owned
handles are awaited to terminal status before executable edits or Git capture.

Checkpoint identity: branch `feature/servicebus-a-plus-api`, annotated tag
`servicebus-a-plus-iteration-119-saga-ownership-checkpoint-2026-09-15`.
The validated packet, manual tests, catalogue and evidence are captured together.
Only exact owned paths are staged; protected review/results and unrelated changes
are excluded. Publication uses a normal atomic branch-and-tag push, never force.
The branch, tag object and peeled commit are verified with a subsequent read-only
`git ls-remote`; the report's accepted bindings must match the committed tree.

## Remaining connected work, explicit non-closure

- Index/query integrity: bare boolean predicates evaluated against null state;
  stale mutable indexed keys, reference-identity membership, duplicate correlation
  IDs, atomic index publication after getter faults, and deferred enumeration.
  Preserve current referenced-state query semantics, not stale-index-only behavior.
- Explicit operation cancellation must reach the saga acquisition as well as the
  dictionary acquisition; factory registration failures need exact owned-lease
  unwind. Linked-token exception normalization requires its own causal tests.
- Required callback/query/probe/context boundaries beyond this packet, query-result
  construction and misleading generic Undo comments remain to be corrected.
- Cross-provider read-only/preinsert/Undo and dispatch-pipe cleanup must receive a
  coherent semantic correction without invented rollback or arbitrary object clones.
  Static Azure Table/DynamoDB/EF reading is not real cloud acceptance.
- RollingTimer disposal/restart/trigger semantics, remaining retry/redelivery gaps,
  actual delayed/RabbitMQ provider acceptance, whole-source personal reading and
  all-host/package/API/journey/isolated-consumer final gates remain active.

The user's requirement “Stelle sicher, dass du den gesamten Code mindesten 1x
komplett gelesen hat” is retained: these nine completed source reads advance that
obligation; they do not establish completion of the entire `src` tree. Requirements
for whole-product A+, no dummy/legacy remnants, every API/parameter test, all comments,
bidirectional async naming and final global coverage likewise remain broader than
this bounded evidence.
