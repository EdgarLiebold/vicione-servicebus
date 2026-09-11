# Iteration 74 — Context Architecture

## Decision

**PASS for the bounded iteration.** All 33 production files that were formerly placed directly in
`src/ViciOne.ServiceBus/Context` were read manually in full, interpreted with their callers and
contracts, reorganized by ownership, hardened, documented, and validated without a known regression.
The repository-wide A+ source goal remains open because the remaining production areas still require
the same file-by-file manual review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `ae059528e710b734c6c884c8d9f877a57399a780` |
| Starting tag | `servicebus-a-plus-remediation-iteration-73-2026-09-11` |
| Candidate product C# aggregate | `375ca8eed263301cc51903940635505a4f8928fb0deb0b3f250f1de7510be174` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-74-2026-09-11` |

The product aggregate was calculated after the final source and test changes and before the
completion commit. Generated files below `bin`, `obj`, `artifacts`, and `TestResults` were excluded.

## Manual review scope and method

Every one of the 33 pre-existing C# files and 3,420 physical lines directly below the former flat
`src/ViciOne.ServiceBus/Context` directory was read in full. The review followed each adapter into
its construction sites, affected transport or capability project, public abstraction, serializer,
and native tests. Directly coupled source files were read where a namespace, accessibility, or
contract change crossed the original directory boundary.

No generator or scripted rewrite authored production code, comments, names, tests, or architectural
decisions. Comments were reviewed only after the corresponding implementation was understood. The
comments in all 33 files now describe current code behavior and contract semantics; they do not
describe migration history or how the code was produced. Mechanical tooling was used only for file
enumeration, compilation, test execution, package/API extraction, formatting, coverage, hashes, and
risk calculations.

## Physical organization and namespace policy

The former flat directory no longer contains source files. Runtime types now live with the capability
or pipeline boundary that owns them:

| Directory | Files | Responsibility |
|---|---:|---|
| `src/ViciOne.ServiceBus/Batching/Contexts` | 1 | Assembled batch consume context |
| `src/ViciOne.ServiceBus/Consumer/Contexts` | 2 | Consumer identity and consumer payload scope |
| `src/ViciOne.ServiceBus/Context/Activities` | 8 | Courier activity context projections and scopes |
| `src/ViciOne.ServiceBus/Context/Consumption` | 5 | General consume-context bases, scopes, and typed projections |
| `src/ViciOne.ServiceBus.Mediator/Contexts` | 2 | Mediator-owned consume and send contexts |
| `src/ViciOne.ServiceBus/Middleware/Contexts` | 2 | Bound-value and correlation middleware projections |
| `src/ViciOne.ServiceBus/RetryPolicies/Contexts` | 2 | Activity retry context state |
| `src/ViciOne.ServiceBus/Scheduling/Contexts` | 2 | Scheduler and scheduled-redelivery adapters |
| `src/ViciOne.ServiceBus/Transactions/Contexts` | 4 | Transaction context contracts and implementation |
| `src/ViciOne.ServiceBus/Transports/Receiving` | 3 | Receive-context and lock adapters |
| `src/ViciOne.ServiceBus/Transports/Sending` | 2 | Send-state implementation and transport-send contracts |

Physical `Contexts` and responsibility folders do not create artificial public API namespaces.
Internal implementation types use the namespace of their owning capability. Existing public context
extension points retain the discoverable `ViciOne.ServiceBus.Context` namespace, while public send
and receive transport types now use the accurate `ViciOne.ServiceBus.Transports` namespace. Native
test folders and namespaces mirror each other exactly.

Each moved file has one filename-matching top-level type. The repository source-naming and native-test
layout architecture gates pass. No empty directory remains below `src`, `tests`, or `benchmarks`.

## Public API and ownership remediation

- Moved `MessageSendContext<T>`, `TransportSendContext`, `TransportSendContext<T>`,
  `ReceiveContextProxy`, `NoLockReceiveContext`, and `TransportReceiveContext` from the misleading
  `Context` namespace into the public `Transports` namespace.
- Moved mediator-specific contexts into the mediator assembly and its `Contexts` namespace.
- Internalized implementation-only batch, bind, consumer, correlation, scheduler, redelivery, retry,
  transaction, mediator, and host-activity adapters. Applications could not use these types without
  depending on internal pipeline mechanics, so they are not valid greenfield extension points.
- Sealed `MessageConsumeContext<TMessage>` because its complete behavior is construction-time
  projection and it has no supported inheritance contract.
- Retained the public general consume and activity proxy/scope types that are genuine extension
  points.
- Updated every product, benchmark, test, and provider reference. No forwarding type or compatibility
  alias was added for the previous namespaces.
- Regenerated the packed public API baseline only after 18 developer journeys and all isolated
  package consumers compiled and ran against freshly built packages.

These are intentional greenfield breaking changes. Feature behavior remains available through its
owning API; only inappropriate exposure of runtime mechanics was removed.

## Semantic remediation

### Consume and payload contexts

- `BaseConsumeContext` now retains one stable outgoing-message view instead of allocating a new
  adapter for every property access.
- `MessageConsumeContext<TMessage>` exposes projected exact and assignable contracts while keeping an
  already available source contract authoritative. This preserves repeated materialization identity
  used by non-replacing transforms without hiding a newly projected contract shape.
- `ConsumeContextScope` tests prove local payload precedence, isolation from its source, stable typed
  message identity, and every factory boundary.
- Bind contexts consistently give the bound value precedence over compatible left-side payloads and
  validate all factories and operands.
- Consumer contexts preserve the source message and payloads while adding an isolated local consumer
  scope.

### Send and receive transport contexts

- `MessageSendContext<TMessage>` now models serializer and serialization state as genuinely optional
  until assigned, rejects null assignment, prevents serializer replacement after body creation, and
  emits a precise serialization failure when no serializer exists.
- Durable admission metadata has an explicit immutable pre-serialization boundary.
- Property-bag reads validate the bag and key, use invariant parsing, accept the complete supported
  native signed and unsigned integral set, and perform checked overflow conversion.
- Enum reads are restricted to actual value-type enums and consistently support native, string, and
  UTF-8 representations.
- Receive proxies forward state, endpoint providers, task registration, payloads, and notifications
  with exact argument identity.

### Scheduler, activity, batch, retry, and transaction contexts

- The context-bound scheduler preserves the receive input address and delegates all 33 advanced
  scheduling and cancellation shapes exactly once with their original arguments.
- Execute and compensate adapters validate source contexts, projected arguments/logs, and activities.
  Tests enumerate every 24 execute-result factory methods plus all compensate factories on both proxy
  and scope implementations.
- Batch contexts expose the immutable ordered snapshot, preserve completion metadata, suppress
  per-item fault settlement as required by collector ownership, validate inputs, and preserve the
  exact cancellation token.
- Retry pending-result markers now describe their actual uncommitted state; unused exception storage
  and constructors were removed.
- System transaction operations reject use after disposal explicitly, retain active state after
  caller cancellation, and make commit, rollback, and disposal terminal and idempotent where defined.

## Test additions and mutation evidence

The core test count increased from 2,612 to 2,640 and the complete unit-solution count increased from
5,145 to 5,174. Twenty-eight new core facts or strengthened contract variants cover message-send
property conversion, message projection, payload scopes, bind precedence, scheduler delegation,
receive forwarding, system transactions, activity factories, batch semantics, and consumer context
identity. One additional architecture fact permanently guards all 33 ownership paths and namespaces.
Every added fact has a matching requirement projection entry, and the exact projection gates pass.

Four independent production mutations were introduced one at a time, rebuilt, detected by the named
test, and manually removed before final validation:

| Mutation | Detecting test | Result |
|---|---|---|
| Removed projected-message participation from `HasMessageType` | `ProjectedMessage_IsVisibleThroughExactAndAssignableTypedLookups` | Killed at the expected assertion |
| Removed right-side payload precedence from bind lookup | `BoundValue_PrecedesLeftPayloadsAndFactories` | Killed; left instance observed instead of right |
| Replaced the consume scheduler's input destination | `EveryAdvancedOperation_DelegatesExactlyOnceWithTheExpectedDestinationAndArgumentsAsync` | Killed; exact destination mismatch |
| Delegated `Completed()` to `Terminate()` | `ExecuteAdapters_ForwardEveryOperationAndBindProjectedArgumentsAsync` | Killed; exact operation mismatch |

The final Release engineering rebuild proves that no mutant remained in a product or test binary.

## Validation

| Gate | Result |
|---|---|
| Engineering solution Release build | PASS — 0 warnings, 0 errors across the complete graph in 4m 28s |
| Source-name and source-layout architecture tests | PASS — the original 10-test gate passed; the added Context ownership fact then passed separately and in both complete architecture runs |
| Complete architecture assembly | PASS — final 255 passed, 0 failed, 0 skipped |
| Complete unit solution | PASS — final 5,174 passed, 0 failed, 0 skipped |
| Complete core coverage run | PASS — 2,640 passed, 0 failed, 0 skipped |
| Requirement projection | PASS as part of the complete core run |
| Developer journeys | PASS — 18 scenarios executed against fresh packages |
| Package consumer isolation | PASS — 31 packages and 3 provider testing consumers |
| Packed public API | PASS — 30 runtime assemblies match the newly committed intentional baseline |
| Packed public API hash | `ad8e0454acd5fd972df79d9a7e07846162af42241e07f0fe8865c4dad09a4415` |
| `dotnet format --verify-no-changes` | PASS; one known non-fatal workspace-load warning, no format change |
| `git diff --check` | PASS |
| SDK pinning | PASS — `global.json` contains only the MTP runner selection and no SDK version |
| Preprocessor directives below `src` | PASS — none |
| MassTransit or stated legacy compatibility markers below `src` | PASS — none |
| Stale old Context type qualifications and test namespace | PASS — none |

The whole-source lexical dummy scan has no `dummy`, `stub`, `TODO`, `FIXME`, `HACK`, or workaround
result. Its three `placeholder` results are real domain behavior: two declarative state-machine
schedule placeholders and one lazy deserialized message-data placeholder. They are not dummy
implementations. This global lexical result is not presented as a substitute for the still ongoing
manual review of production areas outside this iteration.

## Coverage and change risk

Coverage was collected from the complete 2,640-test core module. Compiler-generated state-machine
classes were merged by source file and line for the bounded aggregate.

| Scope | Line coverage | Branch coverage | Instrumented methods | Methods with CRAP > 30 |
|---|---:|---:|---:|---:|
| 33-file Context iteration | 720 / 817 = **88.13%** | 243 / 298 = **81.54%** | 394 | **0** |
| All product code loaded by the core test module | 42,814 / 61,651 = **69.45%** | 14,832 / 23,926 = **61.99%** | Not used as the bounded decision | Not used as the bounded decision |

The highest bounded CRAP score is 12.02. The core aggregate is not represented as complete
repository coverage: it includes product code loaded by the core module but not every provider's
local-integration execution. Repository-wide merged coverage remains a final-goal validation after
all manual source iterations are stable.

Notable bounded file results include 100% line coverage for all eight activity adapters, both
consumer adapters, bind and correlation adapters, the scheduler adapter, both transaction types,
the receive proxy, and the no-lock receive context. `MessageSendContext<TMessage>` is 98.26% line and
90.79% branch covered. Broader base consume/proxy paths remain covered through the complete suite but
are not falsely described as 100%.

## Diagnostic record

- On .NET SDK 10.0.302, two diagnostic project-form `dotnet test` commands built the MTP executable
  successfully but reported zero selected tests. Direct execution of that freshly built MTP test
  application immediately listed and ran the same tests. Final evidence therefore uses the generated
  test executable for focused/coverage runs and the supported `dotnet test --solution ... --no-build`
  form for the complete 5,174-test gate.
- One in-sandbox no-incremental mutation build stopped making progress during compilation. It was
  terminated cleanly, the temporary mutation was immediately reverted, and the same bounded build
  completed reliably outside the sandbox with build servers disabled, one MSBuild worker, and shared
  compilation disabled. All subsequent final builds and tests used that known-safe pattern where
  necessary.

## Repository hygiene and limits

- `review/**` was neither edited nor staged.
- Generated `TestResults/**` coverage data remains untracked and is excluded from the completion
  commit.
- Two empty test directories left by earlier moves were removed from the working filesystem.
- This is implementation-team evidence and does not claim independent external or Red Team
  acceptance.
- Real cloud or broker protocol acceptance was not repeated because this iteration changes context
  ownership and in-process delegation, not provider wire behavior. Provider projects, their unit
  tests, freshly packed packages, and isolated package consumers were nevertheless compiled and
  validated. Real-provider acceptance remains part of the final goal-level validation.
