# Iteration 79 — Abstractions Application Root

## Decision

**PASS for the bounded iteration.** All fifteen original non-generated C# files directly in
`src/ViciOne.ServiceBus.Abstractions` and the owning project file were read manually in full. Their
code, comments, public surface, type grouping, names, namespaces, filenames, directory placement,
and project ownership were assessed together. The project root is now an exact application-contract
boundary; the one embedded runtime implementation was moved to its owning context capability
without changing the packed public API or removing behavior.

The repository-wide A+ source goal remains open. This decision applies only to the completed
Abstractions application-root scope; every remaining production directory still requires the same
manual file-by-file review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `997e94b4c53b02893180b9cbe56c240087a34c3a` |
| Starting tag | `servicebus-a-plus-remediation-iteration-78-2026-09-11` |
| Candidate all-source aggregate | `5c91a396ba79298e2112b276955d22153f094f12404d4ed74c8b2e0409c44604` |
| Candidate bounded C# aggregate | `75c7fc79b352325a4a9c63701ac79475b84539d649007d06700c5d62e3af7ee4` |
| Packed public API SHA-256 | `0f8ce7537b9d2a0bf4b0d82d7b534dd8abe486d7621e21a012cc96390c94688f` |
| Async guard SHA-256 | `9014a87363875e58dc12937cd4b61e6692707c0edafc27a7c2b37ff7467fe318` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-79-2026-09-11` |

The aggregates were calculated from the final source state before the completion commit by hashing
each sorted relative path, a NUL separator, its file bytes, and a terminating NUL. The all-source
aggregate covers 4,188 non-generated files below `src`. The bounded aggregate covers the fifteen
application-root C# files plus the relocated context implementation: 16 files and 846 physical
lines. Generated output below `bin`, `obj`, and `artifacts` was excluded.

## Manual review scope and organization decision

The reviewed root contains application-level contracts: `IBus`, `IBusControl`, `IConsumer`,
`IOutgoingMessages`, `ISendEndpoint`, `ISendEndpointProvider`, `IPublishEndpoint`, `ConsumeContext`,
message headers and limits, and the four application option records. `GlobalUsings.cs` is project
infrastructure. These types correctly occupy the project root and the root `ViciOne.ServiceBus`
namespace. The directory `src/ViciOne.ServiceBus.Abstractions` is the source-project boundary; it is
not a duplicate of a nested `ViciOne.ServiceBus` folder and must not be mechanically flattened or
renamed.

`IOutgoingMessages.cs` was the single ownership violation. It declared both the public interface and
the internal `ConsumeContextOutgoingMessages` runtime implementation. The implementation now lives
at `Context/ConsumeContextOutgoingMessages.cs` in namespace `ViciOne.ServiceBus.Context`; the
contract file declares only its public interface. An exact architecture rule now rejects additional
unreviewed root files, wrong root namespaces, a secondary implementation in the contract file, or a
misplaced context implementation.

No generator or scripted rewrite authored code, comments, tests, names, or architecture decisions.
Comments were evaluated and, where necessary, rewritten only after the implementation and caller
path were understood. Static tooling was used only for enumeration, hashes, builds, test execution,
coverage, formatting, API extraction, and lexical checks.

## Behavior and tests

The five `IOutgoingMessages` operations now have direct evidence for routed and explicit send,
default and configured publish, scheduled send, missing route/scheduler behavior, exact destination,
message, option, due-time, result and cancellation-token forwarding, constructor ownership, every
public null boundary, and zero downstream work after a rejected argument. A real InMemory transport
journey proves routed send, default publish, configured publish, explicit send, response options,
and the diagnostic for an unconfigured route. `MessageLimits.Conservative` has an exact test for its
singleton identity and all five published policy values.

Nine tests were added: seven direct outgoing-message tests, one exact named-policy test, and one
source-ownership architecture test. The existing real-transport journey was strengthened. The
complete Unit solution increased from 5,323 to 5,332 tests.

The following isolated product or structure mutations were each killed and restored before final
validation:

| Mutation | Killing evidence |
|---|---|
| Restored the internal implementation inside `IOutgoingMessages.cs` | Exact declared-type ownership test failed |
| Dropped explicit send options | Direct forwarding test observed `null` instead of the caller instance |
| Bypassed default publication | Publish test observed one call instead of two |
| Shifted scheduled due time by one tick | Exact schedule forwarding assertion failed |
| Removed the implementation constructor null guard | Exact boundary test observed no exception |
| Increased the conservative body limit by one byte | Named-policy value assertion failed |
| Looked up a routed destination for `object` instead of the message type | Real InMemory journey failed with the expected route diagnostic |

An additional route-condition sabotage was rejected by nullable warning-as-error compilation before
execution. It is compiler-gate evidence, not counted as one of the seven runtime/architecture
mutation kills.

## Coverage and API evidence

The final complete core coverage run executed 2,755 tests. Merging compiler-generated async state
machines by source file gives the following executable coverage for the two implementation-bearing
files in this iteration:

| Scope | Line coverage | Branch coverage |
|---|---:|---:|
| `ConsumeContextOutgoingMessages.cs` | 26 / 35 = **74.29%** | 5 / 8 = **62.50%** |
| `MessageLimits.cs` | 28 / 28 = **100.00%** | 12 / 12 = **100.00%** |
| Bounded implementation aggregate | 54 / 63 = **85.71%** | 17 / 20 = **85.00%** |
| All product code loaded by the core module | 43,357 / 61,945 = **69.99%** | 14,979 / 23,901 = **62.67%** |

The separate Abstractions suite passes all 537 tests, including the exact direct boundary matrix.
Its test project does not reference the MTP coverage provider, so both supported collection routes
produced empty coverage packages. No numeric Abstractions coverage is invented from that
instrumentation limitation; direct assertions and isolated mutation kills provide the behavioral
evidence. Dependency changes were not introduced merely to manufacture a metric.

Fresh package validation exercised 18 developer journeys, 31 packages, three isolated provider
testing consumers, and all 30 runtime package APIs. The resulting packed contract remains exactly
19,773 lines with SHA-256
`0f8ce7537b9d2a0bf4b0d82d7b534dd8abe486d7621e21a012cc96390c94688f`.
The source relocation therefore caused no public API drift.

## Final validation

| Gate | Result |
|---|---|
| Final Unit solution Release build | PASS — 0 warnings, 0 errors in 1m 51.67s |
| Complete Unit solution, sequential MTP | PASS — 5,332 passed, 0 failed, 0 skipped in 5m 51.126s |
| Focused changed source-navigation rule | PASS — 1 passed, 0 failed, 0 skipped |
| Core requirement projection | PASS — 1 passed, 0 failed, 0 skipped |
| Abstractions requirement projection | PASS — 1 passed, 0 failed, 0 skipped |
| Focused final Async guard | PASS — 30 passed, 0 failed, 0 skipped in 2m 18.910s |
| Complete core coverage run | PASS — 2,755 passed, 0 failed, 0 skipped in 47.952s |
| Separate Abstractions suite | PASS — 537 passed, 0 failed, 0 skipped |
| Developer journeys and packed API | PASS — 18 journeys, 31 packages, 3 isolated consumers, 30 APIs |
| Changed-project formatting at warning/error severity | PASS |
| `git diff --check` and requirement JSON syntax | PASS |
| Preprocessor directives below `src` | PASS — none |
| Empty source directories | PASS — none |
| Bounded dummy/legacy lexical scan | PASS — no actionable marker |

## Diagnostic record and retained work

- A repository-wide `dotnet format` probe at information severity reported the existing analyzer
  suggestion inventory across benchmark, test, and tool projects and exited with no source changes.
  The bounded production and owning-test projects pass the accepted warning/error formatting gates.
- The Abstractions coverage collector executed all 537 tests but could not initialize a profiler for
  that project. This is recorded as an instrumentation boundary, not represented as zero coverage or
  hidden behind a dependency change.
- Roslyn formatting and some MTP collection paths require local named pipes denied by the filesystem
  sandbox. Their identical approved local executions passed; the sandbox symptom is not a product
  compilation failure.
- While following a related context call path, `Middleware/BasePipeContext.cs` exposed generic or
  stale comment wording. That file lies outside this bounded root iteration and is retained for the
  manual Middleware/Context owner pass rather than being edited without completing its source
  review.
- The earlier batching Red Team findings around managed-batch concurrency admission and
  `BatchConnectHandle` disconnect/partial-batch lifecycle remain assigned to the future cohesive
  batching owner pass.

The protected untracked `review/` tree was neither edited nor staged. Existing untracked
`TestResults/` content was not staged. Temporary coverage output remained below `/private/tmp` or
the ignored test-output boundary.

## Iteration disposition

The Abstractions application root is complete for this manual source iteration. The next cohesive
scope is `src/ViciOne.ServiceBus.Abstractions/Context`, where every file will again be read in full
before comments, behavior, type ownership, namespaces, filenames, or directory structure are
changed. The overall A+ goal remains active.
