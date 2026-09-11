# Iteration 78 — Consumers, Events, Metadata, and Message Data

## Decision

**PASS for the bounded iteration.** Every original production file in `Consumer`, `Events`,
`Metadata`, and `MessageData` was read manually in full, interpreted with its public contracts,
configuration entry points, serializers, initializers, call sites, and tests, then reviewed again
after remediation. The completed implementation has explicit consumer lifetime ownership, immutable
event snapshots, cohesive metadata ownership, deterministic message-data timing and storage
semantics, an intentional public surface, and responsibility-aligned types, namespaces, filenames,
and directories.

The repository-wide A+ source goal remains open. Production areas outside the completed manual
iterations still require the same file-by-file code, comment, type, namespace, and physical-layout
review. This bounded decision therefore does not claim that every file below `src` has already
received the final manual pass.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `f833b806e2033d0647e9dd4b60f54dd7448128df` |
| Starting tag | `servicebus-a-plus-remediation-iteration-77-2026-09-11` |
| Candidate all-source aggregate | `43833a8ce65893b8ed3913990e1dea1c5f3d23bf402c36229319146a996dbf41` |
| Candidate bounded C# aggregate | `4904a838046c4a0c7c3b382cc92010fed929f23775c9c394d16efe0dc3bc18e3` |
| Packed public API SHA-256 | `0f8ce7537b9d2a0bf4b0d82d7b534dd8abe486d7621e21a012cc96390c94688f` |
| Async guard SHA-256 | `9014a87363875e58dc12937cd4b61e6692707c0edafc27a7c2b37ff7467fe318` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-78-2026-09-11` |

The aggregates were calculated from the final source state before the completion commit. The
all-source aggregate covers every non-generated file below `src`; the bounded aggregate covers the
70 final C# files below `Consumers`, `Events`, and `MessageData`. Generated output below `bin`,
`obj`, and `artifacts` was excluded.

## Manual review scope and method

All 70 original files in the four selected production areas were read manually and in full. The
review followed behavior into the public MessageData and consumer contracts, advanced and
configuration entry points, dependency injection, serializers, initializers, topology
conventions, dynamic message implementation, transport admission, and all directly owning tests.
The final three responsibility roots contain 70 C# files and 3,490 physical lines. Related files
outside those roots were also read before their call sites or comments were changed.

No generator or scripted rewrite authored production code, comments, names, tests, or
architectural decisions. Comments were evaluated only after the owning implementation and call
path had been understood. Every comment changed in the bounded production scope now describes
current behavior, lifetime, ownership, conversion, or storage semantics. Mechanical tooling was
used only for enumeration, compilation, test execution, coverage and CRAP calculation, formatting,
API extraction, hashes, and lexical checks.

## Physical organization and namespace policy

| Directory | Files | Responsibility |
|---|---:|---|
| `Consumers` | 4 | Public consumer factories |
| `Consumers/Contexts` | 2 | Consumer-context proxy and scoped ownership |
| `Consumers/Conventions` | 1 | Internal convention-version cache |
| `Consumers/Metadata` | 2 | Consumer metadata discovery and registration snapshot |
| `Events/Faults` | 3 | Fault and receive-fault snapshots |
| `Events/Readiness` | 2 | Bus and host readiness snapshots |
| `Events/Receiving` | 7 | Endpoint and transport lifecycle snapshots |
| `MessageData` | 3 | Public repository implementations |
| `MessageData/Admission` | 1 | Internal transport-boundary admission evidence |
| `MessageData/Configuration` | 12 | Internal get/put transformation composition |
| `MessageData/Conventions` | 8 | Internal topology discovery and application |
| `MessageData/Converters` | 6 | Internal conversion capability and implementations |
| `MessageData/Internals` | 1 | Internal message-data type classification |
| `MessageData/PropertyProviders` | 8 | Internal lazy get/put value projection |
| `MessageData/Serialization` | 2 | Internal wire-reference contracts |
| `MessageData/Values` | 8 | Internal inline, stored, lazy, and empty values |

The singular `Consumer` owner was replaced by `Consumers`, and its contexts, conventions, and
metadata now have matching physical and namespace owners. The flat event directory was divided by
fault, readiness, and receive lifecycle responsibility. The incohesive top-level `Metadata`
directory was removed: consumer registration metadata moved to `Consumers/Metadata`, the
message-data converter seam moved to `MessageData/Converters`, and dynamic interface construction
is now owned by `Internals/Reflection/MessageImplementationCache`.

The public MessageData factory is owned by `Advanced/MessageData.cs`; application repository
selection is owned by `Configuration/MessageDataRepositorySelectorExtensions.cs`. Implementation
values, converters, property providers, topology conventions, and transformation specifications
are internal. No forwarding namespace, compatibility wrapper, duplicate metadata cache, stale
former owner, or empty source directory remains in this bounded scope. Exact architecture tests
reject additions, removals, or misplaced files in the final layout.

## Consumer and event behavior

- Default, delegate, and object factories own the consumer instances they create. Asynchronous
  disposal takes precedence over synchronous disposal and runs after both successful and failed
  consumption. The instance factory never disposes a caller-owned consumer.
- Object factory type failures are translated to the domain-owned `ConsumerException` rather than
  leaking a framework cast exception.
- Convention state is snapshotted and cached by version. Tests that deliberately mutate the
  process-global convention are serialized by an explicit collection, removing the former
  108-of-109 scheduling race.
- Fault and receive-fault events snapshot caller-owned message-type collections and reject null
  elements. Diagnostic values retain wire-safe scalar values and render enumerations predictably.
- Readiness and receive lifecycle events have direct semantic tests for identifiers, addresses,
  tags, metrics, time-provider values, host metadata, exception ownership, and immutable copies.

## Message-data architecture and behavior

- `UseInMemory`, `UseFileSystem`, and `UseEncryption` expose one consistent greenfield composition
  vocabulary. Selector callbacks, repository results, paths, policies, and encryption dependencies
  fail at the API-owned boundary before registration or provider work.
- The in-memory repository honors the supplied retention period through an injected time provider,
  distinguishes both exact expiry boundaries, checks cancellation before work, snapshots stored
  bytes, and generates its address without a separate identifier implementation type.
- The file-system repository validates null and cancellation ownership, preserves round-trip and
  missing-data behavior, and proves that every address-derived path remains below the configured
  root before file I/O.
- Converter inputs and stored values snapshot mutable bytes where required. Stream ownership is an
  explicit converter capability rather than an inference from one concrete implementation.
- Get and put property providers no longer inspect `Task.IsCompleted` followed by `.Result`.
  Synchronous and asynchronous completion, failure, cancellation, empty values, and populated
  values now have identical observable semantics independent of scheduling.
- Addressed values load lazily from their owning repository exactly once. Address, cancellation
  token, returned task, and resolved value identity are directly asserted.
- An addressless populated MessageData value is always admitted by the put provider; the removed
  ambient fallback can no longer silently bypass repository ownership.
- Unsupported array and dictionary shapes fail with actionable configuration messages identifying
  the unsupported member and expected contract.

## API contract

Fresh package validation exercised 18 developer journeys, 31 packages, three isolated provider
testing consumers, and all 30 runtime package APIs. The update run and a separate comparison run
produced the identical 19,773-line packed contract with SHA-256
`0f8ce7537b9d2a0bf4b0d82d7b534dd8abe486d7621e21a012cc96390c94688f`.
The reduced surface reflects deliberate internalization and owner moves, not feature removal.

## Test additions and mutation effectiveness

The focused additions cover factory lifetime ownership, convention cache stability, lifecycle
events, fault snapshots, message implementation caching, repository selection, all converter
families, public API shape, repository retention/path/cancellation contracts, message-data values,
and both property-provider directions. Core and architecture requirement manifests contain the
new exact projections.

The following isolated mutations were each killed by a causally related test and restored before
final validation:

| Mutation | Killing evidence |
|---|---|
| Returned an empty repository result for an addressed lazy value | Lazy-load test failed on resolved value and repository call |
| Inverted addressless put admission | Addressless custom-value test observed missing storage |
| Returned caller-owned stored bytes | Snapshot test observed the later mutation |
| Changed in-memory expiration from `<` to `<=` | Exact TTL-boundary test failed |
| Restored a direct object-consumer cast | Factory boundary leaked `InvalidCastException` instead of `ConsumerException` |
| Added an unexpected file below the exact MessageData layout | Source-navigation architecture test rejected it |
| Removed stream-ownership transfer | Custom converter test observed premature disposal |
| Reused a caller-owned fault type array | Fault snapshot test observed later mutation |
| Replaced a safe diagnostic scalar with its type name | Wire-safe scalar test failed |

The bidirectional Async guard was also adversarially mutation-tested. Permanent sabotage cases
cover wrong suffixes, `AsyncCore`, `Task` and `ValueTask` aliases, generic asynchronous types,
Release-only compile and using items, `NET10_0` branches, source-defined same-FQN types, and Quartz
interface, alias, namespace, overload, generic, static, `ref`, parameter, and return mismatches.
The final guard resolves real metadata symbols and accepts a non-Async Quartz member only when
Roslyn maps the declared class method to the referenced external interface member.

An internal read-only Red Team iterated against the guard and found several real weaknesses:
suffix matching, overly broad Quartz syntax exceptions, overload ambiguity, aliases, Release item
evaluation, same-FQN types, and a derived-interface false positive. Each finding was reproduced,
remediated, and converted into permanent test evidence. On the final unchanged SHA-256
`9014a87363875e58dc12937cd4b61e6692707c0edafc27a7c2b37ff7467fe318`, the Red Team found no
remaining reproducible compilable false positive or false negative in its declared scope. This is
an internal adversarial review, not an independent external acceptance.

## Coverage and change risk

Coverage was collected from 2,755 complete core test executions against the final product source.
Compiler-generated state-machine classes were merged by source file and executable line for the
reviewed capability aggregate.

| Scope | Line coverage | Branch coverage | Instrumented methods | Methods with CRAP > 30 |
|---|---:|---:|---:|---:|
| Reviewed consumer/event/message-data capability | 1,067 / 1,204 = **88.62%** | 461 / 572 = **80.59%** | 246 | **0** |
| All product code loaded by the core test module | **72.50%** | **64.80%** | 15,132 | 198 |

The reviewed capability has no method above CRAP 30. `FaultExceptionInfo.NormalizeDiagnosticValue`
was refactored from cyclomatic complexity 42 to 20 and its scalar/enum behavior was mutation-tested.
The repository-wide loaded-product aggregate is not represented as complete repository coverage;
provider local-integration suites and final merged repository coverage remain separate overall-goal
evidence.

## Final validation

| Gate | Result |
|---|---|
| Final Unit solution Release build | PASS — 0 warnings, 0 errors in 12.67s |
| Complete Unit solution, sequential MTP | PASS — 5,323 passed, 0 failed, 0 skipped in 6m 54.004s |
| Complete architecture assembly | PASS — 289 passed, 0 failed, 0 skipped |
| Focused final Async guard | PASS — 30 passed, 0 failed, 0 skipped |
| Internal Async Red Team | PASS — unchanged hash, no surviving reproducible mutant in scope |
| Complete SQL Transport module | PASS — 126 passed, 0 failed, 0 skipped |
| SQL purge concurrency repetition | PASS — 20/20 after the final test-oracle correction |
| Complete core coverage run | PASS — 2,755 passed, 0 failed, 0 skipped in 48.006s |
| Requirement projections | PASS — exact core and architecture projections |
| Developer journeys and packed API | PASS — 18 journeys, 31 packages, 3 isolated provider consumers, 30 APIs |
| Current NuGet vulnerability inventory | PASS — no vulnerable direct or transitive package reported |
| Unit-solution `dotnet format --verify-no-changes` | PASS — one known non-fatal workspace-load warning |
| `git diff --check` | PASS |
| Requirement JSON syntax | PASS |
| Preprocessor directives below `src` | PASS — none |
| Empty source directories | PASS — none |

The first final solution attempt exposed an unchanged race in the SQL test's `RecordingPipe`:
concurrent `CallCount++` operations could lose one increment after both tasks completed. The product
purge filter was correct. The spy now uses `Interlocked.Increment` and `Volatile.Read`; the case
then passed 20 consecutive repetitions, its full 126-test module, and the final solution run.

## Lexical findings and repository boundaries

No preprocessor directive exists below `src`. The bounded implementation contains no `dummy`,
`stub`, `TODO`, `FIXME`, `HACK`, `NotImplemented`, or MassTransit marker. The whole-source lexical
scan still finds the word `placeholder` in two schedule API comments, where it denotes the real
state-machine schedule declaration, and `NotImplementedException` in the technical-failure
classifier, where it classifies an application exception as non-retryable. Neither is a dummy
implementation. Their owning files remain subject to the repository-wide manual pass rather than
being rewritten from a lexical match alone.

The protected untracked `review/` tree was neither edited nor staged. Existing untracked
`TestResults/` content was not used as commit evidence and must not be staged. Temporary coverage
and mutation output remained below `/private/tmp` or the ignored test-output boundary.

## Diagnostic record

- The reliable .NET 10/MTP whole-solution command separates compilation from test execution and
  uses `dotnet test --solution ... --no-build --max-parallel-test-modules 1`. Passing MSBuild
  properties through that command causes MTP hosts to exit without discovery and is not evidence.
- Running all test modules concurrently once caused an unchanged Saga integration timeout under
  artificial host load. Sequential execution completed the entire solution and is the accepted
  deterministic gate.
- Roslyn formatting and repeated MTP execution require local named pipes that the filesystem
  sandbox denies. The identical read-only commands pass outside the sandbox; the error is recorded
  as an infrastructure boundary rather than misdiagnosed as a compilation or product failure.

## Iteration disposition

Consumer creation, infrastructure events, metadata ownership, and message-data storage are
complete for this bounded manual source iteration. The next iteration must continue across the
remaining source tree file by file. For every file it must understand the implementation before
editing comments, and assess the type name, type responsibility, public surface, namespace,
filename, containing directory, and project ownership together. The overall A+ goal remains active.
