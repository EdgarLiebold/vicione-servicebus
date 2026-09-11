# Iteration 75 — Batching Architecture

## Decision

**PASS for the bounded iteration.** Every pre-existing production file in the batching runtime was
read manually in full, interpreted with its public contracts, configuration path, context adapters,
executors, and native tests, then reorganized and hardened without a known feature regression. The
repository-wide A+ source goal remains open because production areas outside the completed manual
iterations still require the same file-by-file review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `53ecf047fa5c463a095a29ee516f66f720c548a4` |
| Starting tag | `servicebus-a-plus-remediation-iteration-74-2026-09-11` |
| Candidate product C# aggregate | `1dc7e9957a6eaecc2c81a9bdd34cfe5132fe0c44669119ca9d0ebcc96a2b98a4` |
| Candidate batching C# aggregate | `12a1a756aa65c4fed105f573e75a6f7fc7fa9fb0314ba5ea4bec88d9529540ce` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-75-2026-09-11` |

The aggregates were calculated from the final source state before the completion commit. Generated
content below `bin`, `obj`, `artifacts`, and `TestResults` was excluded.

## Manual review scope and method

All seven original batching implementation files and their 1,109 physical lines were read manually
in full. The review also followed the code into `BatchOptions`, the public `Batch<T>` contract,
completion and time-limit enums, grouping-key providers, the registration configurator and connector,
`TaskExecutor`, the in-memory outbox batch projection, JSON type mapping, and all four original
source-mirrored batching test files. The final eight-file, 1,058-line implementation was then read
again in its new form.

No generator or scripted rewrite authored production code, comments, names, tests, or architecture.
Comments were reviewed only after the implementation and its call paths were understood. Every
comment in the bounded production files now describes current behavior, ownership, or contract
semantics; none describes migration history or how the implementation was produced. Mechanical
tools were used only for enumeration, building, test execution, formatting, coverage extraction,
hashing, and lexical checks.

## Physical organization and namespace policy

The former flat `src/ViciOne.ServiceBus/Batching` implementation root contains no C# files. Runtime
state and delivered contexts now have explicit owners:

| Directory | Files | Responsibility |
|---|---:|---|
| `src/ViciOne.ServiceBus/Batching/Runtime` | 6 | Collection, batching, dispatch, factory, settings, and lifecycle ownership |
| `src/ViciOne.ServiceBus/Batching/Contexts` | 2 | Internal delivered-batch contract implementation and consume-context projection |

The namespaces match these folders exactly. Each file has one filename-matching top-level type,
apart from the intentional arity variants in `BatchCollector.cs`. No empty directory remains. The
native tests remain together under `Consumers/Batching`, where they test the capability rather than
copying its private implementation folders.

All batching implementation types remain internal. The architecture gate now rejects any exported
type in a namespace beginning with `ViciOne.ServiceBus.Batching`, inventories all ten internal
runtime types, and verifies the exact directory/namespace mapping. No forwarding type, alias, or
legacy compatibility namespace was introduced.

## Architecture remediation

The two former 200-line grouped and ungrouped collector implementations duplicated admission,
cancellation linking, retry isolation, probing, lifecycle, and completion behavior. They were
replaced by one `BatchCollectorBase<TMessage>` that owns those invariants. The sealed ungrouped and
grouped collectors now own only their respective active-batch lookup state.

Additional architecture corrections are:

- `BatchRuntimeSettings` captures and validates all effective options once at connection time.
  Mutating the configuration object later cannot change an active runtime or its probe output.
- `BatchCollectorLifetime` is the single owner of admission counting, terminal draining, partial
  batch flushing, dispatcher shutdown, and collector shutdown.
- `IBatchCollector.CompleteAsync` no longer accepts an unused consume context.
- Grouped removal is based on completed batch identity and does not evaluate a potentially stateful
  grouping selector a second time.
- The grouping provider is validated before lifetime resources are allocated, and a provider that
  reports a present null key is rejected explicitly.
- `BatchConsumerFactory` captures the same immutable settings snapshot used by runtime behavior.
- Nullable activity and log-context state is represented honestly; null-forgiving placeholders were
  removed.

## Behavioral remediation

### Admission, cancellation, and retry

- An individual message pipeline now waits with its own cancellation token. A canceled member exits
  immediately and is removed from a partial batch instead of waiting for unrelated members.
- Cancellation of the last member closes the empty batch, disposes its timer, releases the buffered
  context, and produces no delivery.
- Collection links the caller and message cancellation tokens while preserving the token that
  actually stopped admission.
- A retry closes the active collection window before the retry is admitted to a successor batch for
  ungrouped, keyed, and null-key fallback streams.
- Repeated message identifiers retain the first admitted context exactly once while every duplicate
  pipeline observes the same terminal batch outcome.

### Ordering and completion

- Delivery order uses the transport sequence number when available and otherwise the message sent
  time. The clock is used only when neither transport value exists.
- Size completion uses a defensive `>=` boundary.
- Time completion uses the injected `TimeProvider` and `Timeout.InfiniteTimeSpan`; both from-first
  and from-last timing retain exact deterministic metadata.
- Batch completion snapshots the ordered contexts, stops timers and registrations, then clears the
  mutable buffer so completed consumers do not retain message contexts.
- The public batch view is a detached array snapshot, validates its completion enum, and implements
  both enumerator shapes over the same ordered data.

### Delivery and failure ownership

- Dispatch-queue cancellation is propagated with the exact admission token to every owned message
  pipeline.
- A batch-pipe cancellation with the owning context token becomes the exact terminal cancellation.
- Dispatcher unavailability faults both size-completion admission and the owned message pipeline.
- Batch-consumer failures reach undelivered messages, while an already delivered transport context
  is not faulted a second time.
- Retry payloads are propagated back to every original context after a batch consumer failure.
- The activity captured during admission is restored while the batch pipe executes.
- Timer stop, timer disposal, and registration cleanup are attempted independently. One failure is
  preserved exactly; multiple independent failures are returned as an ordered aggregate.

## Test-gap analysis and additions

Before remediation, three deliberate mutations were applied independently to the original runtime.
All 29 original batching executions remained green, proving real gaps rather than inferred gaps:

| Original mutation | Original result |
|---|---|
| Removed message ordering before delivery | SURVIVED — 29/29 passed |
| Removed forced closure before retry admission | SURVIVED — 29/29 passed |
| Removed per-member cancellation registration | SURVIVED — 29/29 passed |

The final batching suite has 48 executions, an increase of 19. New and strengthened tests cover:

- transport-sequence and sent-time ordering;
- immediate per-member cancellation, last-member cancellation, exact token identity, and partial
  batch exclusion;
- full-dispatch-queue cancellation and unavailable-dispatcher failure;
- retry isolation for ungrouped, keyed, and ungrouped-fallback modes;
- immutable runtime settings and released completed buffers;
- selector single evaluation and illegal present-null grouping keys;
- duplicate identifier retention and completion of both message pipelines;
- delivered versus undelivered failure settlement;
- batch-pipe cancellation and admission activity restoration;
- timer cleanup single and aggregate failures;
- constructor, factory collaborator, wrong-message-type, and disposal boundaries;
- defensive batch snapshots, completion-mode validation, and consume-context notification contracts.

Every new test method has a source-owned requirement projection. The complete projection gates pass.

## Mutation evidence

Nine final-production mutations were introduced one at a time, rebuilt, killed by the named test,
and manually removed before the final build:

| Mutation | Detecting test | Result |
|---|---|---|
| Removed ordered projection of buffered messages | `CompletedBatch_IsOrderedByTransportSequenceOrSentTimeFallbackAsync` | KILLED for both ordering sources |
| Removed forced closure before retry admission | `RetryAdmission_ClosesTheActiveBatchBeforeDeliveringTheRetryAsync` | KILLED in all three collector modes |
| Removed message cancellation registration | `CancelingOneMember_CancelsOnlyItsPipelineAndExcludesItFromTheBatchAsync` | KILLED; canceled item remained in delivery |
| Removed the message-token-bound completion wait | `CancelingOneMember_CancelsOnlyItsPipelineAndExcludesItFromTheBatchAsync` | KILLED by the bounded wait |
| Removed completed-buffer clearing | `Collector_CapturesOptionsAndReleasesBufferedContextsAfterCompletionAsync` | KILLED; two contexts remained retained |
| Replaced the immutable settings snapshot with live options | `Collector_CapturesOptionsAndReleasesBufferedContextsAfterCompletionAsync` | KILLED by post-connection mutation |
| Reversed already-delivered fault suppression | `DeliveryFailure_IsSuppressedOnlyForAnAlreadyDeliveredContextAsync` | KILLED in both theory cases |
| Replaced the first context for a duplicate identifier | `DuplicateMessageIdentifier_IsRepresentedOnceWhileBothPipelinesCompleteAsync` | KILLED; sequence 2 replaced sequence 1 |
| Marked canceled dispatch admission as successful | `CanceledDispatchAdmission_CancelsTheOwnedPipelineWithTheAdmissionTokenAsync` | KILLED; terminal cancellation disappeared |

An initial form of the final mutation was rejected by warnings-as-errors because it left the caught
exception unused. That compiler-killed form is not counted as mutation evidence; it was replaced by
the buildable behavioral mutant listed above. The final Engineering build proves no mutant remains
in product or test binaries.

## Coverage and change risk

Coverage was collected from all 2,659 core test executions against the final Release build.
Compiler-generated state-machine classes were aggregated by source file.

| File | Line coverage | Branch coverage | Instrumented methods |
|---|---:|---:|---:|
| `BatchConsumeContext.cs` | 16 / 17 = **94.12%** | 7 / 8 = **87.50%** | 5 |
| `MessageBatch.cs` | 13 / 14 = **92.86%** | 2 / 2 = **100.00%** | 5 |
| `BatchCollector.cs` | 112 / 113 = **99.12%** | 44 / 44 = **100.00%** | 21 |
| `BatchCollectorLifetime.cs` | 48 / 51 = **94.12%** | 16 / 24 = **66.67%** | 12 |
| `BatchConsumer.cs` | 155 / 169 = **91.72%** | 63 / 74 = **85.14%** | 24 |
| `BatchConsumerFactory.cs` | 22 / 22 = **100.00%** | 6 / 6 = **100.00%** | 4 |
| `BatchRuntimeSettings.cs` | 14 / 14 = **100.00%** | 6 / 6 = **100.00%** | 3 |
| **Executable batching aggregate** | **380 / 400 = 95.00%** | **144 / 164 = 87.80%** | **74** |

`IBatchCollector.cs` contains no executable body and therefore has no instrumented lines. No bounded
method has a CRAP score above 30. The highest score is 16.00 for fully line-covered `AddAsync`, whose
cyclomatic complexity is 16. The remaining uncovered lifecycle branches are defensive secondary
executor-disposal and callback-race paths; the reachable timer, registration, queue, cancellation,
and delivery failure paths are directly exercised. Coverage is supporting evidence, not a claim that
line execution alone proves correctness.

## Validation

| Gate | Result |
|---|---|
| Final Engineering solution Release build | PASS — 0 warnings, 0 errors |
| Complete batching suite | PASS — 48 passed, 0 failed, 0 skipped |
| Complete core coverage run | PASS — 2,659 passed, 0 failed, 0 skipped |
| Complete architecture assembly | PASS — 256 passed, 0 failed, 0 skipped |
| Complete unit solution | PASS — 5,194 passed, 0 failed, 0 skipped in 5m 23.916s |
| Requirement projection | PASS as part of the complete core and architecture runs |
| Developer journeys | PASS — 18 scenarios executed against freshly packed packages |
| Package consumer isolation | PASS — 31 packages and 3 provider testing consumers |
| Packed public API | PASS — 30 runtime assemblies match the committed baseline |
| Packed public API hash | `ad8e0454acd5fd972df79d9a7e07846162af42241e07f0fe8865c4dad09a4415` |
| `dotnet format --verify-no-changes` | PASS — one known non-fatal workspace-load warning, no format change |
| `git diff --check` | PASS |
| Batching preprocessor directives | PASS — none |
| Batching dummy, stub, TODO, FIXME, HACK, workaround, or placeholder markers | PASS — none |
| Stale flat Batching namespaces | PASS — none |
| Empty Batching directories | PASS — none |

The packed public API baseline is unchanged because the reorganized batching implementation is
internal. All public batching functionality remains available through the existing greenfield
contracts and configuration surface.

## Diagnostic record

An in-sandbox `dotnet format` attempt failed with `SocketException (13): Permission denied` while
Roslyn tried to create its local named-pipe endpoint. The same command was rerun immediately outside
the filesystem sandbox and passed. Reliable build commands used build servers disabled, one MSBuild
worker, and shared compilation disabled where applicable. This is the established solution for this
workspace's sandbox IPC restriction, not a source or dependency failure.

## Repository boundaries and limits

- `review/**` was neither edited nor staged.
- Generated `TestResults/**` coverage output remains untracked and is excluded from the completion
  commit.
- No real cloud or broker acceptance was repeated because this iteration changes the in-process
  batching runtime and internal ownership, not provider protocol behavior. All provider projects,
  unit tests, fresh packages, and isolated package consumers were nevertheless rebuilt and tested.
- This is implementation-team evidence and does not claim independent external or Red Team
  acceptance.
- The repository-wide manual source review remains in progress; this report certifies only the
  completed batching capability and its directly coupled boundaries.
