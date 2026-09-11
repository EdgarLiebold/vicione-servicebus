# Iteration 72 — Core Transport Architecture

## Decision

**PASS for the bounded iteration.** The non-fabric core transport implementation was manually reviewed, reorganized by cohesion, hardened, and validated without a known regression. This decision does not close the repository-wide A+ source goal: the remaining product projects and the cross-cutting public-context naming decision still require their own manual passes.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `36cd0b1ccd179567a247de5065b7a9ae8be87ce6` |
| Starting tag | `servicebus-a-plus-remediation-iteration-71-2026-09-11` |
| Candidate product C# aggregate | `efdab99035af5036b804420cd47b2702d13fcd43aea47db25daf778a9247e53f` |
| Candidate iteration-scope aggregate | `f2519b3272a516d66a3640708f74011c75914b4ab29c9eab602f8b3c0764f3e6` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-72-2026-09-11` |

The candidate aggregates were calculated after the final source and test changes and before the completion commit. Generated files under `bin`, `obj`, and `artifacts` were excluded.

## Manual review scope and method

Every one of the 76 pre-existing C# files below `src/ViciOne.ServiceBus/Transports`, excluding the already completed `Fabric` subtree, was read in full and interpreted in its caller, lifecycle, and provider context. The new `SendEndpointResourceRelease.cs` file was manually authored and reviewed, producing a final bounded scope of 77 C# files and 7,889 physical lines. No generator or scripted rewrite authored production code, comments, names, or architectural dispositions.

Comments were reviewed only after the corresponding implementation had been understood. Generic, stale, historical, humorous, and construction-process descriptions were replaced with precise current English descriptions of behavior and contracts. The manually encountered unprofessional `IMessageSerializer` summary in the adjacent abstraction contract was also corrected.

Mechanical tools were used only for file enumeration, hashes, compilation, tests, API extraction, formatting verification, coverage collection, and CRAP calculation.

## Physical organization

The former flat transport directory was divided into cohesive implementation areas while preserving the single `ViciOne.ServiceBus.Transports` namespace where that namespace remains the coherent public contract:

| Directory | Files | Responsibility |
|---|---:|---|
| `Addressing` | 1 | Endpoint-address identity and hashing |
| `Diagnostics` | 7 | Metrics, health, logging, observers, and idle notification |
| `Formatting` | 9 | Message names, routing keys, and partition keys |
| `Headers` | 2 | Transport-header admission and JSON mapping |
| `Hosting` | 16 | Host, bus, rider, and transport-supervisor lifecycle |
| `Receiving` | 22 | Endpoint dispatch, receive locks, retries, and consumer ownership |
| `Sending` | 17 | Send transports, endpoints, provider caches, pipes, and resource ownership |
| `Components/KillSwitch` | 3 | Failure-rate circuit control |

The obsolete empty `Transports/Contexts` directory was removed. Architecture tests that intentionally inspect source paths were updated to the new locations. No empty directory remains in the bounded transport tree.

## Architectural and semantic remediation

### Addressing, formatting, headers, and diagnostics

- Replaced the mutable-looking `AddressEqualityComparer` utility with the sealed singleton `EndpointAddressComparer` and made equality and hash-code normalization agree.
- Replaced implementation-shaped delivery metrics with the `IDeliveryMetrics` contract, renamed the concurrency metric to `MaxConcurrentDeliveryCount`, and propagated the contract through all transport providers.
- Renamed `ZeroActiveDispatchHandler` to the behavior-oriented `ZeroActivityHandler` and isolated subscribers so one failing idle callback cannot suppress later subscribers or corrupt a successful delivery.
- Corrected nested closed-generic message-name formatting so a nested type contributes only the generic arguments that it owns and an open constructed type is rejected deterministically.
- Added complete null, cancellation, non-finite-number, delegate-result, and key-format boundary guards where contracts previously leaked null-reference or incidental failures.

### Hosting and riders

- Made host startup rollback explicit when synchronous or asynchronous startup fails.
- Preserved caller cancellation while separating it from transport completion and cache-owned lifetime cancellation.
- Serialized concurrent rider generation ownership, made failed stop attempts retryable, and scoped rider lookup to the active generation with case-insensitive names.
- Replaced implicit lifecycle assumptions with explicit starting, ready, paused, stopping, stopped, and faulted transitions where applicable.

### Receiving

- Made `ConsumerAgent` own and observe the first registered consume-loop task, distinguish graceful from unexpected completion, and stop automatically after an unexpected terminal outcome.
- Serialized `PendingReceiveLockContext` terminal settlement so concurrent completion, fault, and cancellation cannot settle the same retained delivery more than once.
- Refactored `ReceivePipeDispatcher` into explicit dispatch, settlement, observer-notification, and failure-recording phases. Null task contract violations are normalized; observer failures cannot replace the primary pipeline failure; a settlement failure preserves both causes.
- Made receive-endpoint and collection start/stop failures retryable without losing registrations, and prevented synchronous transport completion from being overwritten by a later `Starting` state write.
- Split the retry loop in `ReceiveTransport` into named retry-preparation, completion-reporting, and retry-context operations, reducing the former high-risk state-machine complexity without changing terminal semantics.

### Sending and provider ownership

- Separated cache-owned endpoint creation cancellation from individual caller wait cancellation. A canceled first caller no longer cancels shared creation; disposing the cache does.
- Normalized provider contract violations for null normalization results, acquisition tasks, transports, send contexts, and observer handles, and proved that failed cache entries remain retryable.
- Added `SendEndpointResourceRelease` so observer disconnection and asynchronous transport disposal are both attempted. Creation, disconnect, and disposal failures are retained in deterministic order rather than one cleanup failure hiding another.
- Memoized endpoint disposal so concurrent or repeated disposal executes owned cleanup exactly once and returns the same terminal outcome.
- Refactored the physical send path into context creation, dispatch, diagnostics, and fault notification. A fault observer cannot replace the actual send failure.
- Corrected typed/untyped send-pipe composition after the full test suite exposed an incorrect single-execution assumption: an object implementing both contracts now executes each distinct configured pipeline stage exactly once.

## Public API and naming disposition

The iteration removed public exposure from implementation-only dispatcher and endpoint-provider classes, replaced implementation-shaped metrics and callback names, and regenerated the packed API baseline. The packed contract contains 30 assemblies and 20,308 lines with SHA-256 `625eb701efae5f3725bda9859d10828d0ed4a39b0518097eae73a058558afe17`.

Interfaces such as `ReceiveEndpointContext`, `ReceiveLockContext`, `ReceiveTransportHandle`, and `SendTransportContext` belong to the repository-wide context/handle taxonomy that also includes `PipeContext`, `SendContext`, `PublishContext`, and `ConsumeContext`. They were deliberately not renamed in isolation: a partial `I`-prefix conversion would make the API less consistent. Their final greenfield disposition remains a cross-cutting public-API task for the full source pass and is not represented as completed by this bounded iteration.

## Test additions and strengthened proof

The core test count increased from 2,534 to 2,559. New or materially strengthened tests cover:

- formatter ownership of nested generic arguments and open-type rejection;
- rider lookup and concurrent generation ownership;
- consumer-agent normal, canceled, faulted, and unexpected terminal outcomes;
- send-endpoint cache creation-token and disposal ownership;
- publish and send provider happy paths, invalid contracts, retries, and cleanup failures;
- receive dispatch primary-failure, settlement-failure, and idle-subscriber isolation;
- send context creation, pipeline order, terminal lifecycle, and fault-observer isolation;
- kill-switch inclusive limits plus NaN, infinity, and out-of-range ratios;
- concurrent receive-lock settlement and fallback delivery behavior;
- dual typed/untyped pipe composition.

Requirement projection entries were added for every new fact or theory so the executable evidence remains tied to explicit variants.

## Validation

| Gate | Result |
|---|---|
| Core project build | PASS — 0 warnings, 0 errors |
| Core test project | PASS — 2,559 passed, 0 failed, 0 skipped |
| Engineering solution build | PASS — 0 warnings, 0 errors |
| Unit solution build | PASS — 0 warnings, 0 errors |
| Complete unit solution | PASS — 5,092 passed, 0 failed, 0 skipped |
| Bidirectional async and cancellation architecture gates | PASS as part of the complete architecture test assembly |
| Developer journeys | PASS — 18 scenarios, 31 freshly packed packages, 3 isolated provider testing consumers |
| Packed public API | PASS — all 30 runtime package APIs match the committed baseline |
| `dotnet format --verify-no-changes` | PASS; one known non-fatal workspace-load warning, no format changes |
| `git diff --check` | PASS |
| Preprocessor directives in bounded scope | PASS — none |
| Dummy, stub, placeholder, TODO, FIXME, legacy, MassTransit, or `NotImplementedException` markers in bounded scope | PASS — none |
| Empty directories in bounded scope | PASS — none |

The Microsoft Testing Platform command accepts `--minimum-expected-tests` but does not expose `--maximum-expected-tests`. Supplying the latter was diagnosed as the cause of an otherwise misleading zero-test run. All final validation uses only the supported minimum gate.

## Coverage and risk result

Coverage was collected by the complete 2,559-test core module and calculated only from repository product paths for the aggregate below. Generated sources and test infrastructure were not counted as product coverage.

| Scope | Line coverage | Branch coverage | Methods with CRAP > 30 |
|---|---:|---:|---:|
| All product code instrumented by the core module | 40,357 / 56,612 = **71.29%** | 14,101 / 22,266 = **63.33%** | Not used as the bounded decision |
| Full `src/ViciOne.ServiceBus/Transports` tree, including the iteration-71 Fabric scope | 2,808 / 3,199 = **87.78%** | 998 / 1,374 = **72.63%** | **0** |

The complete transport tree contains 59 instrumented files and 682 methods. Two hundred fifty methods fall below an individual 80% line or 70% branch threshold, including trivial accessors and unexercised adapter branches; this inventory remains risk evidence and is not hidden by the aggregate result. The highest transport CRAP score is 30.00 for the fully line-covered `BaseReceiveEndpointContext` constructor. The three former CRAP-above-30 asynchronous state machines were structurally reduced and now have no successor above 30.

This iteration did not run a mutation campaign and does not claim mutation closure. Repository-wide mutation testing remains a final goal-level validation after all manual source iterations are stable, avoiding invalidation by later architectural changes.

## Repository hygiene and limits

- `review/**` was neither edited nor staged.
- Generated `TestResults/**` coverage data remains untracked and is excluded from the completion commit.
- The iteration is an implementation-team review and does not claim independent external or Red Team acceptance.
- Provider-specific real-broker acceptance was not repeated because this iteration did not change broker protocols; those suites remain part of the final provider and acceptance passes.
