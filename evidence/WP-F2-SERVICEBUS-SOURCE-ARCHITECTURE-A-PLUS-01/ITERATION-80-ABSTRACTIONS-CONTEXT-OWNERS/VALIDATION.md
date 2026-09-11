# Iteration 80 — Abstractions Context Owners

## Decision

**PASS for the bounded iteration.** The ten original files and 1,485 physical lines in
`src/ViciOne.ServiceBus.Abstractions/Context` were read manually in full. Their code, comments,
contracts, callers, visibility, namespaces, filenames, directory placement, and project ownership
were assessed together. The mixed directory has been replaced by explicit public-SPI, internal
runtime, and Core-runtime owners, and the old directory is absent.

The repository-wide A+ source goal remains open. This decision applies only to the completed
Abstractions context scope and its moved implementations; the remaining production tree still
requires the same manual file-by-file review.

## Candidate identity

| Item | Value |
|---|---|
| Repository | `repositories/vicione-servicebus` |
| Branch | `feature/servicebus-a-plus-api` |
| Starting commit | `755a771cd249c8730eab5e43b43066973a02050b` |
| Starting tag | `servicebus-a-plus-remediation-iteration-79-2026-09-11` |
| Candidate all-source C# aggregate | `6d209195e94930d7eaf4eb177cf82fc92abb4ab1f0f3b46c44c6c36910ad6a3c` |
| Candidate bounded C# aggregate | `10ed9713a29c2f35653e0d482c8d87cfb803929f4392fd402cb3738d2c26bda9` |
| Packed public API SHA-256 | `eeef563f54d8dc551467fa19bda58c69caa2991e4c9e0e6ca0688dcb1f489866` |
| Async guard SHA-256 | `9014a87363875e58dc12937cd4b61e6692707c0edafc27a7c2b37ff7467fe318` |
| Planned completion tag | `servicebus-a-plus-remediation-iteration-80-2026-09-11` |

The aggregates hash each sorted relative path, a NUL separator, its file bytes, and a terminating
NUL. The all-source aggregate covers 4,112 non-generated C# files below `src`. The bounded
aggregate covers the fourteen final implementation files plus the three directly participating
Advanced interfaces: 17 files and 1,999 physical lines. Generated output below `bin`, `obj`, and
`artifacts` was excluded.

## Manual organization and comment review

The old directory combined three different architectural layers:

- `PublishContextProxy`, `SendContextProxy`, and `SendContextScope` are intentional extension SPI.
  They now occupy `Advanced/Contexts` and namespace `ViciOne.ServiceBus.Advanced` beside the context
  contracts they extend.
- Runtime-type endpoint conversion and application-options adaptation are implementation details.
  They now occupy `Internals/Dispatching` and `Internals/Outgoing`, have functional names, and are
  internal.
- The consume outgoing facade, unavailable consume sentinel, and pending-fault collector are Core
  runtime types. They now occupy `Context/Consumption` or `RetryPolicies` in the Core project and
  are internal.

`OutgoingOptionsPipe.cs` previously declared five independent top-level types. Each final type now
has one matching file. The public cache-named types were not retained as compatibility facades:
applications already compose endpoint contracts, while every first-party implementation caller is
a named friend of Abstractions. Removing these accidental exports therefore removes legacy surface
without feature loss.

Every comment in the original scope and final implementation files was checked only after its code
and call path were understood. Proxy summaries now describe forwarded metadata and pipe-context
behavior; dispatcher summaries describe runtime contract mapping rather than their cache; Core
types describe their actual state and notification semantics. No generator or scripted rewrite
authored source code, comments, tests, names, or architecture decisions. Static tooling was used
only for inventory, hashes, builds, tests, coverage, formatting, API extraction, and lexical checks.

The project directories directly below `src` and the capability directories directly below
`src/ViciOne.ServiceBus` are distinct levels: the former are build/project ownership, while the
latter are Core capability ownership. This distinction is valid, but every remaining project and
capability placement is still subject to the open repository-wide manual review; no blanket claim
is made from directory names alone.

## Behavior corrections and direct tests

`SendContextProxy.CreateProxy` previously asked its wrapped parent to create the typed view. A
derived `SendContextScope` could therefore lose its local payload layer during a message-type
conversion. It now creates the replacement over the current proxy, matching publish semantics and
retaining the complete active view.

`PendingFaultCollection.NotifyAsync` previously enumerated observer calls directly into
`Task.WhenAll`. A synchronous observer exception stopped enumeration and skipped later faults. Each
notification is now converted to a task independently; synchronous exceptions and invalid null
tasks are represented as faulted tasks, and every collected fault is attempted before completion.

The former six catch-all context tests were replaced or relocated into responsibility owners.
Direct evidence now covers all send/publish/response dispatcher overloads, runtime type validation,
all proxy state, typed replacement, payload precedence and mutation ownership, every unavailable
consume-context member family, pending-fault lifecycle and failures, and all outgoing-option types,
fields, snapshots, probes, and capability failures. Test counts changed as follows:

| Module | Before | After | Delta |
|---|---:|---:|---:|
| Abstractions | 537 | 541 | +4 |
| Core | 2,755 | 2,773 | +18 |
| Architecture | 290 | 292 | +2 |
| Complete Unit solution | 5,332 | 5,356 | +24 |

## Mutation evidence

Each mutation below was introduced alone, observed failing, and immediately restored:

| Mutation | Killing evidence |
|---|---|
| Created a typed send proxy through the wrapped parent | `CreateProxy_PreservesTheCurrentViewAndRequiresAMessage` rejected the bypass |
| Redirected `ConversationId` to `CorrelationId` | Complete proxy property matrix reported the exact differing GUID |
| Dropped the caller cancellation token from send dispatch | Exact dispatcher invocation reported the token mismatch |
| Re-exported `SendEndpointDispatcher` | API-layer architecture test reported the exact forbidden public type |
| Enumerated pending observer calls without synchronous-failure isolation | Later-fault and null-task tests failed on missing notification/wrong failure |
| Resolved the parent payload before the local payload | Scope precedence test reported parent instead of local identity |
| Retained caller-owned option headers instead of freezing them | Send, publish, schedule, and request snapshot tests observed the caller mutation |
| Ignored pre-cancellation in the unavailable sentinel | Token-operation matrix observed the domain failure instead of cancellation |

During restoration of the property mutation, a broad matching patch initially changed the adjacent
getter and temporarily swapped correlation and conversation reads. The mandatory final manual
reread detected this before final validation. The getters were corrected explicitly, then the
complete build, complete tests, formatting, package gate, and coverage passed. This intermediate
error is retained here because it is direct evidence for the manual final-read requirement.

## Coverage and public API evidence

The final instrumented Core run executed all 2,773 Core tests. Compiler-generated async classes
were merged by source path and line before calculating the bounded values:

| Core file | Line coverage | Branch coverage |
|---|---:|---:|
| `ConsumeContextOutgoingMessages.cs` | 35 / 35 = **100.00%** | 8 / 8 = **100.00%** |
| `UnavailableConsumeContext.cs` | 63 / 63 = **100.00%** | 4 / 4 = **100.00%** |
| `PendingFaultCollection.cs` | 38 / 38 = **100.00%** | 8 / 8 = **100.00%** |
| Bounded Core aggregate | 136 / 136 = **100.00%** | 20 / 20 = **100.00%** |
| All product code loaded by the Core module | 43,435 / 61,950 = **70.11%** | 14,985 / 23,903 = **62.69%** |

The Abstractions test project has no MTP coverage-provider reference. Its 541-test profile and six
independent Abstractions behavior/API mutations pass, but no numeric Abstractions percentage is
invented from unavailable instrumentation.

The packed API changed deliberately from 19,773 to 19,701 lines. The five public proxy/scope types
retain their functionality under the coherent `ViciOne.ServiceBus.Advanced` namespace;
`SendContextProxy` now explicitly implements its actual `SendContext` contract. The unavailable
sentinel, pending-fault collector, and three converter-cache types disappear from the public API.
No compatibility aliases remain. The reviewed contract has 65 inserted and 137 removed lines and
SHA-256 `eeef563f54d8dc551467fa19bda58c69caa2991e4c9e0e6ca0688dcb1f489866`.

## Final validation

| Gate | Result |
|---|---|
| Unit-solution Release build | PASS — 0 warnings, 0 errors in 2m 10.51s |
| Complete sequential Unit solution | PASS — 5,356 passed, 0 failed, 0 skipped in 5m 32.687s on the final candidate |
| Complete Architecture assembly | PASS — 292 passed, 0 failed, 0 skipped in 3m 00.630s within the final solution run |
| Bidirectional Async profile | PASS — 30 passed, 0 failed, 0 skipped in 2m 34.855s |
| Complete final Core coverage run | PASS — 2,773 passed, 0 failed, 0 skipped in 1m 16.056s |
| Focused final unavailable-sentinel profile | PASS — 4 passed, 0 failed, 0 skipped |
| Developer journeys and packed API | PASS — 18 journeys, 31 packages, 3 isolated consumers, 30 APIs |
| Changed-project formatting at warning/error severity | PASS — no diagnostics or edits |
| `git diff --check` and requirement JSON syntax | PASS |
| Preprocessor directives below `src` | PASS — none |
| Empty source directories | PASS — none |
| Bounded dummy/legacy lexical scan | PASS — no actionable marker |

## Diagnostic record and retained work

- The in-sandbox package-consumer run packed all packages, then failed only because DNS access to
  NuGet.org was denied. The identical approved local run passed completely.
- `dotnet format` blocked inside the sandbox at the MSBuild named-pipe boundary. The same project
  validators passed outside the sandbox without changing files.
- MTP coverage failed before test discovery with `SocketException (13)` while creating its named
  pipe. Both identical approved local coverage runs passed; the second closed the one initially
  uninstrumented concrete sentinel entry and produced the final 100% bounded result.
- Generic/stale wording in `Middleware/BasePipeContext.cs` remains queued for its cohesive manual
  Middleware pass. The earlier managed-batch concurrency and `BatchConnectHandle` lifecycle
  findings likewise remain assigned to their future batching owner pass.

The protected untracked `review/` tree was neither edited nor staged. Existing untracked
`TestResults/` content was not staged. Coverage artifacts remain below `/private/tmp` and are not
part of the repository candidate.

## Iteration disposition

The former Abstractions context bucket is complete. The next iteration begins with an explicit
manual inventory of the `src` project topology and then continues through the remaining
`ViciOne.ServiceBus.Abstractions/Advanced/Contexts` files. This directly adjudicates the apparent
mix of project directories below `src` and capability directories inside `ViciOne.ServiceBus`
without assuming that either level is correct merely because it currently compiles. The overall
A+ source goal remains active.
