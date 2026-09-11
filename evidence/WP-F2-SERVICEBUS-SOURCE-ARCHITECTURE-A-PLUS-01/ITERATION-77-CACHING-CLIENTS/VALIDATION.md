# Iteration 77 — Caching and Request-Client Architecture

## Decision

**PASS for the bounded iteration.** Every production file in the core cache and request-client
scope was read manually in full, interpreted with its public contracts, owners, call sites, and
tests, then reviewed again after remediation. The bounded implementation has deterministic cache
and client lifetimes, genuinely absolute request deadlines, explicit asynchronous factory
ownership, fail-fast API boundaries, and responsibility-aligned client folders and namespaces.
The repository-wide A+ source goal remains open because source areas outside the completed manual
iterations still require the same file-by-file review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `2ba0efc560c5cea6491d3708c16698397c19d2c3` |
| Starting tag | `servicebus-a-plus-remediation-iteration-76-2026-09-11` |
| Candidate all-source aggregate | `f012199bd60c71b223a72652923472b55a61832ce49c336827189069db2a1d58` |
| Candidate bounded C# aggregate | `156b5ba36ac0a4bba80f6100b30985fb9effa4009f8e5052405cbda0f384219e` |
| Packed public API SHA256 | `7a63fd620a3dedc925a4a3409d419905171388458a0ca78ef482e2579466fb7a` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-77-2026-09-11` |

The aggregates were calculated from the final source state before the completion commit. The
all-source aggregate covers every file below `src`; the bounded aggregate covers the final 34 C#
files below the core `Caching` and `Clients` capability directories. Generated output below `bin`,
`obj`, `artifacts`, and `TestResults` was excluded.

## Manual review scope and method

All 17 cache files and all 17 original client files were read manually in full. The review followed
their behavior into `IClientFactory`, request and response contracts, scoped-client composition,
mediator consumers, dependency injection, cache owners, transport endpoints, and all 13 directly
owning test files. The final bounded implementation contains 34 files and 3,496 physical lines.

No generator or scripted rewrite authored production code, comments, names, tests, or
architectural dispositions. Comments were reviewed only after the corresponding code and call path
had been understood. Every comment changed in this iteration now describes current runtime
behavior, ownership, or contract semantics. The stale `EmptyConnectHandle` description was also
corrected while following disposal ownership. Mechanical tools were used only for enumeration,
builds, tests, formatting, coverage and CRAP calculation, hashes, API extraction, and lexical
checks.

## Physical organization and namespace policy

| Directory | Files | Responsibility |
|---|---:|---|
| `src/ViciOne.ServiceBus/Caching` | 13 | Cache contracts, options, facade, resource ownership, lifecycle, observation, and statistics |
| `src/ViciOne.ServiceBus/Caching/Implementation` | 4 | Internal entry, index, key-projection, and pending-creation mechanics |
| `src/ViciOne.ServiceBus/Clients` | 2 | Client-factory lifetime and scoped public composition |
| `src/ViciOne.ServiceBus/Clients/Contexts` | 3 | Bus, host, and receive-endpoint client-factory contexts |
| `src/ViciOne.ServiceBus/Clients/Endpoints` | 5 | Send and publish request-endpoint adapters |
| `src/ViciOne.ServiceBus/Clients/Requests` | 7 | Request handle, response matching, and handler connection lifetime |

The former flat client directory mixed factories, contexts, endpoint adapters, and request state.
Each internal type now has a physical folder and namespace matching its responsibility. Only
`ClientFactory.cs` and `ScopedClientFactory.cs` remain in the client root. The core project root
contains only project infrastructure: `GlobalUsings.cs`, the project file, and the package lock.
Architecture tests inventory the exact layout and reject a return to the former flat namespace.
No forwarding namespaces, compatibility wrappers, duplicate implementation types, or empty source
directories remain in the bounded scope.

## Cache architecture and behavior

- `ResourceCache.AddIndex` now enters the active-operation lifetime before projecting live
  resources. Cache disposal cannot dispose a resource while the index selector is still using it.
- Expiration observation is mode-specific. Sliding expiration subscribes to the usage source;
  absolute expiration does not create an irrelevant, observable event subscription.
- Invalidated pending creation remains protected from stale completion, and separate tests prove
  removal and clearing through `KeyedResourceCache` release exactly the intended owner state.
- Cache observer and disposal paths retain their feature behavior while race ownership is explicit.

## Request-client API and lifetime

- `IClientFactory` now derives from `IAsyncDisposable`, so temporary response-endpoint ownership is
  present in the public contract rather than only on one concrete implementation.
- Correlated factory overloads require non-null consume contexts. Explicit no-context overloads
  continue to provide the intended uncorrelated entry point.
- `ClientFactory` validates required arguments before endpoint acquisition or wrapped-factory work,
  disposes its owned context exactly once under repeated or concurrent disposal, and rejects new
  client creation once disposal has begun.
- `ScopedClientFactory`, mediator implementations, and advanced composition extensions validate at
  their owning API boundary and use the correct contextual or context-free branch.
- An absolute `RequestOptions.Deadline` is snapshotted at API entry. Remaining time is recomputed
  after endpoint acquisition and configuration, so those delays cannot extend the response timeout
  or an implicit transport time-to-live. An explicit caller TTL remains independently owned.
- Terminal request cleanup uses `TaskScheduler.Default`. A hostile, non-pumping synchronization
  context cannot strand cancellation or fault completion in infrastructure code.
- Endpoint and initializer paths that merely returned an existing task no longer allocate trivial
  async state machines. All user-visible asynchronous contracts retain their behavior and naming.
- `ResponseHandlerConnectHandle`, endpoint contexts, message responses, and both consume-pipe
  forwarding forms have exact lifetime and metadata-forwarding assertions.

## Boundary and test additions

The focused profile grew from 145 to 190 tests. New coverage includes:

- 20 exhaustive `ClientFactory` null-boundary cases and 7 scoped-factory boundary cases;
- idempotent and concurrent async disposal, post-disposal rejection, and hostile synchronization
  context completion;
- endpoint-delay deadline enforcement and implicit-versus-explicit TTL ownership;
- null exception rejection at response completion;
- five receive-endpoint context cases covering projection, connection forwarding, addressed and
  publish readiness, host disposal, and constructor boundaries;
- cache invalidation while synchronous key projection is blocked;
- exact `Dispose` and `Disconnect` forwarding by response-handler handles;
- independent keyed-cache `Remove` and `Clear` forwarding/release behavior;
- exact configured and unconfigured consume-pipe forwarding by the bus context;
- exact `HostInfo` forwarding by `MessageResponse` and its integration consumer.

All new behaviors have explicit requirement metadata. Both the core and architecture requirement
projection tests passed against the embedded JSON manifests.

## Mutation effectiveness

The following isolated product or architecture mutations were each killed by a causally related
test and immediately restored:

| Mutation | Killing evidence |
|---|---|
| Removed the active-operation guard from synchronous cache index projection | Blocking selector observed premature resource disposal |
| Subscribed absolute expiration to resource usage | Exact subscription-count assertion failed |
| Removed the request message guard | Exhaustive boundary test reached unintended dependency work |
| Disposed the owned client-factory context repeatedly | Exact async-disposal count failed |
| Relaxed the deadline zero boundary | Expired request did not fail at the API-owned boundary |
| Reused the original duration instead of remaining deadline time | Delayed endpoint/TTL assertion failed |
| Restored ambient synchronization-context scheduling | Hostile-context cancellation remained queued |
| Returned one client internal to the flat folder | Exact physical-layout inventory failed |
| Removed addressed response-endpoint readiness gating | Receive-endpoint context readiness test failed |
| Made host response-endpoint disposal a no-op | Exact owned-disposal assertion failed |
| Removed pending invalidation cleanup | Cache creation/invalidation ownership assertion failed |
| Made response-handler `Dispose` skip disconnection | Exact disconnect-forwarding assertion failed |
| Made keyed-cache `Remove` a no-op | Separate remove/release assertion failed |
| Made keyed-cache `Clear` a no-op | Separate clear/release assertion failed |
| Dropped options from configured consume-pipe forwarding | Exact context-forwarding assertion failed |
| Removed response `HostInfo` forwarding | Exact metadata assertion failed |
| Disabled direct-interface discovery in the public API extractor | Exact architecture API-contract guard failed |

All mutations were restored before final build, test, API, and formatting validation.

## Public API contract hardening

The new `IClientFactory : IAsyncDisposable` relationship exposed a pre-existing blind spot in the
packed-public-API extractor: it listed type declarations and members but omitted direct interface
relationships. A baseline could therefore remain unchanged after a real public type-contract
change.

The extractor now emits sorted, direct, externally visible interface relationships while excluding
interfaces inherited through a base class or parent interface. The architecture gate asserts both
the extraction algorithm and the exact line
`TYPE interface public ViciOne.ServiceBus.Advanced.IClientFactory
[interfaces=System.IAsyncDisposable]`. Removing direct-interface discovery was mutation-tested and
killed by this guard.

The update run and a separate comparison run each executed 18 fresh-package developer journeys,
31 packages, three isolated provider consumers, and all 30 runtime package APIs. Both independently
produced the same 19,961-line contract and SHA-256
`7a63fd620a3dedc925a4a3409d419905171388458a0ca78ef482e2579466fb7a`.
The 2,075 changed `TYPE` lines record newly visible direct-interface metadata across the existing
surface; they do not represent 2,075 newly added public APIs.

## Coverage and change risk

Coverage was collected from all 2,721 core test executions against the final Release build.
Compiler-generated state-machine classes were merged by source file and executable line for the
bounded aggregate.

| Scope | Line coverage | Branch coverage | Instrumented methods | Methods with CRAP > 30 |
|---|---:|---:|---:|---:|
| 34-file caching/client iteration | 1,152 / 1,246 = **92.46%** | 372 / 428 = **86.92%** | 234 | **0** |
| All product code loaded by the core test module | 40,231 / 55,491 = **72.50%** | 14,242 / 22,082 = **64.50%** | 15,132 | 204 |

All 28 instrumented bounded files executed. `KeyedResourceCache`, `BusClientFactoryContext`, both
receive-endpoint contexts, all addressed/publish endpoint adapters, `MessageResponse`, and
`ResponseHandlerConnectHandle` have complete line execution. The highest bounded CRAP values are
24.054 for the creation-completion state machine, 22.547 for `GetOrAddAsync`, and 20.119 for request
sending.

The loaded-product aggregate is not represented as complete repository coverage. It is only the
product code loaded by the core test module. Provider local-integration suites and final
repository-wide merged coverage remain separate final-goal evidence.

## Validation

| Gate | Result |
|---|---|
| Final `ViciOne.ServiceBus.slnx` Release build | PASS — 0 warnings, 0 errors in 1m 09.40s |
| Complete core native-MTP coverage run | PASS — 2,721 passed, 0 failed, 0 skipped in 1m 28.169s |
| Focused caching/client profile | PASS — 190 passed, 0 failed, 0 skipped |
| Complete architecture assembly | PASS — 260 passed, 0 failed, 0 skipped in 3m 43.045s |
| Requirement projections | PASS — exact core and architecture projections each passed |
| Developer journeys | PASS — 18 scenarios executed against freshly packed packages |
| Package consumer isolation | PASS — 31 packages and 3 provider testing consumers |
| Packed public API | PASS — all 30 runtime assembly contracts match |
| Packed public API hash | `7a63fd620a3dedc925a4a3409d419905171388458a0ca78ef482e2579466fb7a` |
| Locked product/test restores | PASS |
| Current NuGet vulnerability inventory | PASS — no vulnerable direct or transitive package reported |
| Unit-solution `dotnet format --verify-no-changes` | PASS — one known non-fatal workspace-load warning, no format change |
| `git diff --check` | PASS |
| Requirement JSON syntax | PASS |
| Preprocessor directives below `src` | PASS — none |
| Empty source directories | PASS — none |

## Lexical findings and retained follow-up

No `dummy`, `stub`, `TODO`, `FIXME`, `HACK`, `NotImplemented`, or MassTransit marker exists in the
bounded implementation. The whole-source `placeholder` match in
`MessageData/Values/DeserializedMessageData.cs` names a real lazy deserialization state, not a
dummy implementation.

The whole-source compatibility scan identifies two deliberately unadjudicated names outside this
iteration: `LegacyAzureDiagnosticId` in diagnostic propagation and `legacyCanonical` in the QoS
topology validator. Their existence is recorded for their owning manual source iterations; this
bounded decision neither assumes that they are necessary nor changes code that was not read in its
complete owner context.

## Diagnostic record

- The repository's current Microsoft Testing Platform layout discovers tests through direct
  execution of the built native MTP host. `dotnet test --project` discovered zero tests and is not
  accepted as evidence for this profile.
- Sandboxed MTP/Roslyn execution is subject to the established local named-pipe restriction.
  Reliable commands use serial MSBuild, disabled build servers/shared compilation, and the approved
  outside-sandbox path for native test and formatting IPC.
- Coverage and CRAP calculations used temporary read-only analysis below `/private/tmp`; no script
  authored or modified production code or comments.

## Repository hygiene and boundaries

The protected `review/` tree was neither edited nor staged. Existing untracked `TestResults/`
content is not iteration evidence and must not be staged. Temporary coverage and mutation outputs
were written below `/private/tmp`. The completion commit contains only the reviewed production,
tests, dependency locks, tooling, API contract, test-agent records, and this evidence scope.

## Iteration disposition

Caching and request-client ownership are complete for the bounded manual source review. The next
iteration must select the next coherent unreviewed production capability, read every source file
manually, review every comment with its implementation, follow public contracts and tests, and
repeat the same remediation, mutation, coverage, build, format, package, API, commit, tag, and
remote-verification cycle.
