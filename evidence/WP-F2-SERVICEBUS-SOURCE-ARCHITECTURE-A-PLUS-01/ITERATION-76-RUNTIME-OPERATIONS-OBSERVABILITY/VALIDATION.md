# Iteration 76 — Runtime, Operations, and Observability Architecture

## Decision

**PASS for the bounded iteration.** Every original production file in the runtime, hosting,
operations, monitoring, and logging scope was read manually in full, interpreted with its call
sites, public contracts, provider integrations, and native tests, then reviewed again after the
changes. Test-only text-writer logging was followed into its new testing-package owner. The
repository-wide A+ source goal remains open because production areas outside the completed manual
iterations still require the same file-by-file review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `5b51cd5aab44865d0fb0a866c43929c548e2d051` |
| Starting tag | `servicebus-a-plus-remediation-iteration-75-2026-09-11` |
| Candidate all-source aggregate | `9698fb615209d1a59d9f08fe7e9fe6b3025f6a88e2e1e3f577f853ac84a30282` |
| Candidate bounded C# aggregate | `236e638cdbd114c9931ba3be93c2f5c1a8b62ca852fb3a232690e6aaabf56339` |
| Packed public API SHA256 | `ea6e133bf7089d79e5642ba6d351d6a9bbae222ec1ffae64052811ced8bb2a31` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-76-2026-09-11` |

The aggregates were calculated from the final source state before the completion commit. The
all-source aggregate covers every file below `src`; the bounded aggregate covers the final 33 C#
files listed by the capability directories below. Generated output below `bin`, `obj`, `artifacts`,
and `TestResults` was excluded.

## Manual review scope and method

The original scope comprised 36 production files and 3,949 physical lines. All were read manually
in full. The review followed the code into runtime construction and lifecycle callers, dependency
injection, health-check registration, durable-sender admission and delivery, all affected
transport/provider telemetry call sites, benchmark startup, testing facilities, API extraction,
and the original and replacement tests. The final implementation contains 33 files and 3,687
physical lines and was read again in its resulting structure.

No generator or scripted rewrite authored production code, comments, names, tests, or architectural
dispositions. Comments were assessed only after the corresponding implementation and call path had
been understood. Every comment in the bounded production files describes current behavior,
ownership, or contract semantics; none records migration history or construction process.
Mechanical tools were used only for enumeration, builds, tests, formatting, coverage and CRAP
calculation, hashes, API extraction, and lexical checks.

## Physical organization and namespace policy

| Directory | Files | Responsibility |
|---|---:|---|
| `src/ViciOne.ServiceBus/Runtime` | 2 | Bus identity, forwarding, lifecycle, readiness, and cleanup ownership |
| `src/ViciOne.ServiceBus/Hosting` | 1 | Generic-host lifecycle adaptation and background-start observation |
| `src/ViciOne.ServiceBus/Operations` | 5 | Probe contracts, scopes, health projection, and result construction |
| `src/ViciOne.ServiceBus/Operations/ReliableMessaging` | 2 | Durable-sender operational query and health-check behavior |
| `src/ViciOne.ServiceBus/Monitoring/Health` | 2 | Bus health checks and health-check option registration |
| `src/ViciOne.ServiceBus/Monitoring/Telemetry` | 4 | Durable telemetry values, activity scopes, and instruments |
| `src/ViciOne.ServiceBus/Monitoring` | 1 | Stable public telemetry schema |
| `src/ViciOne.ServiceBus/Logging/Diagnostics` | 4 | Internal activity creation, observation, completion, and propagation headers |
| `src/ViciOne.ServiceBus/Logging/Internal` | 3 | Internal log-context and logger-factory implementation |
| `src/ViciOne.ServiceBus/Logging/Monitoring` | 3 | Internal metric binding, instruments, tags, and operation lifetime |
| `src/ViciOne.ServiceBus/Logging` | 3 | Deliberate public logging contracts only |
| `src/ViciOne.ServiceBus.Testing/Logging` | 3 | Text-writer test logger, factory, and options |

Physical folders and namespaces now match. Health-check implementations no longer sit in the flat
monitoring root, reliable-messaging operations no longer sit in a general operations root, and bus
runtime/hosting types no longer sit in the package root. Each bounded file has a filename-matching
top-level type, with the intentional `ServiceBusRuntime` partial split named after its owner.
Architecture tests inventory the exact runtime, health, hosting, and reliable-messaging locations
and reject stale flat namespaces. No empty source directory remains.

The text-writer logger is a testing capability, not a runtime logging API. It therefore moved from
the core package to `ViciOne.ServiceBus.Testing.Logging`; the public registration extension and
options remain available from the testing package while the logger and factory implementations are
internal. No compatibility forwarding types or duplicate namespaces remain.

## Public and internal API architecture

The core package no longer exports implementation mechanics as accidental API:

- `BusLogContext`, `SingleLoggerFactory`, category names, and operation-name constants are internal;
- activity observation, started-activity ownership, propagation headers, and activity extensions
  are internal diagnostics infrastructure;
- the former parallel `DiagnosticActivityExtensions` and `DiagnosticHeaders` surfaces are removed;
- test-writer types are owned by the testing package rather than the production runtime;
- `EnabledLogger` is sealed and can be obtained through `ILogContext` without publicly exposing its
  construction;
- `ServiceBusTelemetry` is the single public, stable schema for activity source, meter, attributes,
  events, instruments, messaging-system values, and reliability outcomes.

All affected core, saga, courier, job-service, persistence, scheduler, and transport callers use the
new internal owners. Fresh package consumers prove the intentional packed surface rather than
relying on source-project internals. The committed API contract changed deliberately and all 30
runtime package APIs match it.

## Runtime and hosting architecture

`ServiceBusRuntime` now has one lifecycle owner and direct dynamic pipe registration. The former
196-line `ServiceBusRuntime.Readiness.cs` implementation and its terminal-fault observer were
removed after the complete call chain proved the waiting branch unreachable:

1. `StartHostHandle.Ready` completes only after all receive-endpoint readiness tasks complete.
2. `ServiceBusRuntime.StartCoreAsync` publishes `_busHandle` only after `busHandle.Ready` succeeds.
3. During startup `_busHandle` is absent; after startup the receive endpoint is already ready.
4. Consequently the old `_busHandle != null` readiness wait could never wait for an incomplete
   endpoint and added synchronous blocking, timeout races, and terminal-observer state without a
   reachable feature.

Both `ConnectConsumePipe` variants and request-pipe registration now delegate directly to the
consume pipe. An architecture rule rejects reintroduction of `TaskBlocking` into the runtime partial
files. A real started InMemory bus test connects both pipe variants and proves message delivery.

The runtime startup path preserves caller cancellation, bounds failure cleanup independently, and
cannot transition to Started after cancellation wins. `ServiceBusHostedService` observes a
background startup task, reports late faults, uses host startup cancellation correctly, and does not
leave unobserved failures. Lifecycle comments state current ownership and timeout semantics.

Previously untouched untyped runtime publish overloads now have real-bus coverage. The tests execute
message-instance, explicit-message-type, anonymous-values, untyped-pipe, and CancellationToken
forwarding. Pre-canceled tokens must surface the original token and must not publish a message.

## Operations, health, and durable messaging

- Reliable-messaging operations and the durable-sender health check share an explicit capability
  namespace and folder.
- Durable sender health distinguishes quarantine backlog, stale work, and hard store bounds rather
  than flattening all conditions into one state.
- Store failures expose a stable health description without leaking provider exception text.
- Caller cancellation is rethrown with its original cancellation semantics instead of being
  translated into an unhealthy result.
- Reliable-messaging dependency injection exposes one validated provider/operation ownership model;
  provider acceptance and admission behavior are exercised with real registrations.
- Operational probe scopes preserve structured values and deterministic health projection.

## OpenTelemetry and logging architecture

Instrumentation now uses one current semantic vocabulary. Deprecated `messaging.operation`,
`exception.escaped`, `peer.address`, and the former `messaging.vicione-servicebus.*` namespace are
absent. Send, receive, and process spans use distinct operation names/types and current messaging
system, destination, processor, message, error, and exception attributes. Header propagation is
internal and carries activity identity, trace state, and filtered baggage without duplicating
conversation or correlation identifiers.

Message-system normalization uses an immutable, ordinal-case-insensitive alias table and maps every
supported transport identity to the fixed public schema. Unknown non-empty systems map to `other`;
null or empty systems map to `unknown`. This replaced a CRAP-76 switch while preserving all aliases
and eliminating lowercase-string allocation.

Send activity population is decomposed into named responsibilities for inherited trace state,
baggage, identifiers, addresses, custom tags, start failure, and header propagation. The former
CRAP-52.86 method no longer exists. Listener callbacks remain an observational boundary: creation,
start, mutation, event, status, stop, metric recording, and secondary logging failures cannot alter
message delivery.

The test text-writer logger validates its writer, clock, category, minimum level, and category
suppression inputs. It emits deterministic invariant entries for every log level, serializes
concurrent writes, treats the writer as caller-owned, and rejects external provider injection rather
than pretending to compose unsupported behavior.

## Requirement and test additions

The iteration adds or strengthens explicit requirements for:

- current activity outcome and exception-event semantics;
- exact log-level mapping and log-category ownership;
- text-writer logger format, suppression, concurrency, input bounds, and writer ownership;
- current OpenTelemetry names, tags, outcomes, and messaging-system normalization;
- durable sender registration, admission, health, and cancellation behavior;
- bounded runtime startup cleanup and observed background startup failure;
- non-blocking dynamic pipe registration and both pipe option paths;
- untyped publish overload and CancellationToken forwarding;
- generic activity sampling, lifecycle, kind, and hostile-listener isolation;
- exact physical source inventory for the reorganized capability folders.

All new tests carry requirement metadata and are present in the compiled requirement projections.
The original tests were retained only where they still prove distinct behavior; obsolete terminal
observer tests were deleted with the unreachable implementation.

## Mutation effectiveness

Three deliberate mutations initially survived the pre-iteration tests and established real gaps:

| Initial mutation | Pre-remediation result |
|---|---|
| Replaced the messaging operation attribute with an obsolete key | SURVIVED |
| Removed activity Error status when an exception event is added | SURVIVED |
| Mapped the information convenience logger to Warning | SURVIVED |

Tests were then strengthened before the candidate was accepted. The following buildable product
mutations were each killed by a causally related test and immediately removed:

| Mutation | Killing evidence |
|---|---|
| Used the wrong current operation-type attribute/value | Exact activity-schema assertions failed |
| Removed Error status from `StartedActivity` | Exception outcome test failed |
| Mapped information logging to Warning | Exact six-level mapping test failed |
| Reintroduced unbounded readiness waiting | Bounded canceled-startup test failed |
| Stopped observing the background startup task | Late-background-fault test failed |
| Rendered Critical entries with the Error code | All-level text-writer test failed |
| Swallowed caller cancellation in durable health | Health cancellation test failed |
| Made hosted-service fault detachment a no-op | Terminal observation test failed |
| Inserted synchronous `TaskBlocking` into dynamic pipe registration | Architecture contract failed |
| Disconnected a newly registered consume pipe immediately | Real InMemory delivery test timed out |
| Mapped RabbitMQ to `other` in the alias table | Complete identity normalization test failed |
| Propagated the caller instead of the started send activity | Exact trace-parent assertion failed |
| Replaced an untyped publish CancellationToken with `None` | Canceled-overload test observed no exception |

The final Engineering build after all mutation reversions proves no mutant remains in compiled
product or test binaries.

## Coverage and change risk

Coverage was collected from all 2,676 core test executions against the final Release build.
Compiler-generated state-machine classes were merged by source file and executable line for the
bounded aggregate.

| Scope | Line coverage | Branch coverage | Instrumented methods | Methods with CRAP > 30 |
|---|---:|---:|---:|---:|
| 33-file runtime/operations/observability iteration | 1,502 / 1,602 = **93.76%** | 558 / 651 = **85.71%** | 258 | **0** |
| All product code loaded by the core test module | 42,974 / 61,664 = **69.69%** | 14,841 / 23,837 = **62.26%** | Not used as the bounded decision | Not used as the bounded decision |

The highest bounded CRAP score is 29.50 for the startup state machine. The removed unreachable
readiness method formerly scored 187.50. The former CRAP-76 normalization switch and CRAP-52.86 send
activity method were replaced by smaller named decisions; no successor exceeds 30.

Notable final results include 100% line coverage for all three test-writer types, both bus-health
types, `StartedActivity`, internal log-context/factory ownership, the durable-sender health check,
and all basic operations helpers. `ServiceBusRuntime.cs` improved to 72/75 lines. Remaining bounded
gaps are enumerated in the raw coverage analysis and are primarily defensive observation-failure,
disposal, alternate outbox, and startup branches; they are not hidden by the aggregate result.

The loaded-product aggregate is not represented as complete repository coverage. It is the product
code loaded by the core test module; provider local-integration suites and final repository-wide
merged coverage remain separate final-goal evidence.

## Validation

| Gate | Result |
|---|---|
| Final Engineering solution Release build | PASS — 0 warnings, 0 errors in 2m 46.17s |
| Complete core coverage run | PASS — 2,676 passed, 0 failed, 0 skipped in 1m 15.875s |
| Complete architecture assembly | PASS as part of the complete unit solution |
| Complete unit solution | PASS — 5,214 passed, 0 failed, 0 skipped in 4m 52.483s |
| Requirement projections | PASS as part of complete core and architecture execution |
| Developer journeys | PASS — 18 scenarios executed against freshly packed packages |
| Package consumer isolation | PASS — 31 packages and 3 provider testing consumers |
| Packed public API | PASS — 30 runtime assemblies match the committed baseline |
| Packed public API hash | `ea6e133bf7089d79e5642ba6d351d6a9bbae222ec1ffae64052811ced8bb2a31` |
| `dotnet format --verify-no-changes` | PASS — one known non-fatal workspace-load warning, no format change |
| `git diff --check` | PASS |
| Requirement JSON syntax | PASS |
| SDK version pinning | PASS — none; `global.json` selects only Microsoft Testing Platform |
| Preprocessor directives below `src` | PASS — none |
| MassTransit or stated legacy compatibility markers below `src` | PASS — none |
| Empty source directories | PASS — none |

The whole-source lexical dummy scan has no `dummy`, `stub`, `TODO`, `FIXME`, `HACK`, or workaround
result. Its three `placeholder` results are current domain behavior: two state-machine schedule
declarations and one lazy deserialized message-data value. They are not dummy implementations. This
lexical scan supports but does not replace the still ongoing manual review of source areas outside
this iteration.

## Diagnostic record

- An in-sandbox Microsoft Testing Platform coverage attempt failed before discovery with
  `SocketException (13): Permission denied` while creating a local named-pipe server. The exact same
  test application was rerun outside the sandbox and passed all 2,676 tests. This is the established
  workspace IPC restriction, not a product or dependency failure.
- The local environment does not provide PowerShell. Coverage and CRAP analysis therefore used a
  temporary read-only Ruby analyzer implementing the documented Cobertura merge and
  `complexity² × (1 - coverage)³ + complexity` formula. It did not author or modify repository code.
- One early focused architecture invocation requested three tests but contained one incorrect
  method name; MTP correctly returned its minimum-test-count violation after the two matching tests
  passed. The correctly named projection test was immediately run and passed, followed by the full
  architecture assembly in the 5,214-test solution gate.
- Reliable builds disabled build servers, used one MSBuild worker, and disabled shared compilation
  where applicable. Final test and format commands used the known-safe outside-sandbox path for MTP
  and Roslyn IPC.

## Repository hygiene and boundaries

The protected `review/` tree was neither edited nor staged. Existing untracked `TestResults/`
content is not iteration evidence and must not be staged. Temporary coverage and mutation outputs
were written below `/private/tmp`. The final commit must contain only the reviewed production,
tests, benchmark, documentation, and this evidence scope.

## Iteration disposition

Runtime lifecycle, generic-host ownership, operational probes, durable-sender operations and
health, core health checks, activity diagnostics, metric instrumentation, telemetry schema, core
logging ownership, and text-writer testing logging are complete for the bounded manual source
review. Iteration 77 must select the next coherent unreviewed production capability, read every file
manually, follow its contracts and tests, and repeat the same review, mutation, coverage, build,
format, package, API, commit, tag, and remote-verification cycle.
