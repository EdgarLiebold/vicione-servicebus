# Iteration 55: Agent and Supervisor Lifecycle

Date: 2026-09-09

Branch: `feature/servicebus-a-plus-api`

Starting point: `servicebus-a-plus-remediation-iteration-54-2026-09-08`

## Objective

This iteration reviewed the Agent/Supervisor and pipe-context lifecycle as one coherent ownership
subsystem. The review covered behavior, concurrency, cancellation, failure propagation, public API
shape, type and file naming, namespaces, physical source and test layout, comments, and direct test
quality. No comment or API generator was used.

The public lifecycle SPI had been split between `ViciOne.ServiceBus.Middleware`,
`ViciOne.ServiceBus.Agents`, root `ViciOne.ServiceBus.Advanced` types, and context types in another
folder. The resulting API was difficult to discover and its physical layout did not match its
conceptual ownership. The reviewed family now has one public namespace and one matching directory:
`ViciOne.ServiceBus.Advanced.Middleware`.

## Personally read source inventory

Every file below was read in full before its behavior or comments were accepted:

### Abstractions lifecycle

- `Advanced/Middleware/Agent.cs`
- `Advanced/Middleware/AgentExtensions.cs`
- `Advanced/Middleware/IAgent.cs`
- `Advanced/Middleware/ISupervisor.cs`
- `Advanced/Middleware/StopContext.cs`
- `Advanced/Middleware/StopSupervisorContext.cs`
- `Advanced/Middleware/Supervisor.cs`
- `Internals/Extensions/TaskExtensions.cs`

### Core pipe-context lifecycle

- `Advanced/Middleware/ActivePipeContext.cs`
- `Advanced/Middleware/ActivePipeContextAgent.cs`
- `Advanced/Middleware/AsyncPipeContextAgent.cs`
- `Advanced/Middleware/AsyncPipeContextFilter.cs`
- `Advanced/Middleware/AsyncPipeContextHandle.cs`
- `Advanced/Middleware/AsyncPipeContextPipe.cs`
- `Advanced/Middleware/ConstantPipeContextHandle.cs`
- `Advanced/Middleware/IActivePipeContextAgent.cs`
- `Advanced/Middleware/IActivePipeContextHandle.cs`
- `Advanced/Middleware/IAsyncPipeContextAgent.cs`
- `Advanced/Middleware/IAsyncPipeContextHandle.cs`
- `Advanced/Middleware/IPipeContextAgent.cs`
- `Advanced/Middleware/IPipeContextFactory.cs`
- `Advanced/Middleware/IPipeContextHandle.cs`
- `Advanced/Middleware/PipeContextAgent.cs`
- `Advanced/Middleware/PipeContextDisposer.cs`
- `Advanced/Middleware/PipeContextSupervisor.cs`
- `Advanced/Middleware/SupervisorExtensions.cs`

Transport call sites affected only by the namespace and BCL task-property migration were updated
mechanically and compiled in both complete solution graphs. They are not represented as separate
file-by-file semantic review cohorts by this evidence.

## Architecture and API corrections

- Consolidated the complete lifecycle family under `ViciOne.ServiceBus.Advanced.Middleware`.
- Moved source and direct tests into directories matching that namespace and responsibility.
- Renamed `ActivePipeContextHandle<T>` to the interface-shaped
  `IActivePipeContextHandle<T>` and aligned its file name.
- Removed the old public lifecycle namespaces and paths from productive source.
- Bound the new layout and the absence of the old API names with architecture tests.
- Replaced the custom `IsCompletedSuccessfully()` helper with the BCL task property at all call
  sites and retained exact cancellation-token transfer in `TaskExtensions`.
- Updated the public API guide and the packaged public API baseline.

## Lifecycle correctness corrections

### Agent

- Concurrent callers share one active stop attempt.
- A caller cancellation cancels only that caller's wait; it does not abandon the owned shutdown.
- Failed or canceled stop attempts clear the shared attempt before publishing their outcome and can
  be retried without a race window.
- A successful stop remains idempotent even for a later already-canceled caller.
- `Stopping` is signaled when the first attempt is accepted; `Stopped` is signaled only after
  successful resource release.
- Exceptions thrown by external `Stopping` or `Stopped` cancellation callbacks cannot prevent,
  fail, or permanently poison the owned stop operation.
- Readiness and completion task transfers preserve the exact terminal state and cancellation token.
- The latest pending transfer source wins, while a completed source remains authoritative.
- Faults from later rejected readiness or completion source tasks are explicitly observed, including
  faults that arrive after rejection.
- Required public and protected inputs reject `null` consistently.

### Supervisor

- Agent signal tasks are validated before registration.
- Registration after stopping begins is rejected atomically inside the agent-set lock.
- Multiple child stops are started before awaiting their combined result.
- Faulted or canceled child completion is retained and remains visible to every later supervisor
  stop attempt; only successful child completion removes the child.
- Faulted child tasks are observed immediately and their exact failure is propagated by shutdown.

### Pipe-context ownership

- Pending, ready, faulted, borrowed, and owned handles are released through explicit lifecycle
  ownership paths.
- A context created after its asynchronous handle or agent has already stopped is rejected and
  disposed instead of being leaked.
- `PipeContextAgent` shares an active disposal attempt but starts a new attempt after a failure.
  `Completed` remains pending until ownership was actually released successfully.
- The complete retry path works through a parent `Supervisor`; a child is no longer removed after a
  failed release.
- The internal `PipeContextDisposer` gives all reviewed handles one consistent preference for
  `IAsyncDisposable` over `IDisposable`.
- Runtime failure before creation faults both acquisition and lifecycle signals where applicable.
- Caller cancellation cannot skip borrowed or owned cleanup, and independent cleanup groups begin
  without one failure suppressing the other.
- Null factory results and supervisor completion without a published creation outcome fail
  explicitly.
- Creation, cancellation, fault publication, background transfer, and late-disposal failures have
  an observing task owner.

## Comment, naming, and hygiene review

Comments in the personally read inventory were checked against the actual implementation and
rewritten manually where necessary. They describe current contracts and functionality, not the
history of how the code was produced. No comment-generation script was used.

The reviewed scope contains:

- no dummy, TODO, FIXME, HACK, placeholder, or temporary-workaround marker;
- no convenience compiler directive;
- no old productive `ViciOne.ServiceBus.Agents` or split lifecycle namespace;
- no file/type mismatch found by the repository architecture gates;
- no bidirectional async-naming violation;
- no diff whitespace error.

Historical evidence and changelogs were not rewritten merely because they record old paths.

## Direct validation

Final focused Release tests after every deliberate mutation had been restored:

- Agent, Supervisor, and TaskExtensions: 24 passed, 0 failed, 0 skipped.
- Pipe-context lifecycle and supervisor creation: 37 passed, 0 failed, 0 skipped.
- API, file layout, background ownership, documentation, async naming, and source hygiene: 34
  passed, 0 failed, 0 skipped.

The first full-suite run before the internal Red Team correction reported 4,616 passing tests and
four architecture/projection failures. Those failures identified one old source path, generated-style
return descriptions, and an incorrectly named async test helper. All four were corrected and their
focused 26-test architecture set passed. They were not accepted as a green validation run.

The final `ViciOne.ServiceBus.Tests.Unit.slnx` Release run used Microsoft Testing Platform v2, one
test module at a time, and a hard minimum of 4,625 discovered tests:

- 4,625 passed;
- 0 failed;
- 0 skipped;
- duration: 4 minutes 39 seconds.

Strict Release builds with warnings treated as errors:

- `ViciOne.ServiceBus.Tests.Unit.slnx`: 0 warnings, 0 errors.
- `ViciOne.ServiceBus.Engineering.slnx`: 0 warnings, 0 errors.

`dotnet format --verify-no-changes --no-restore --severity warn` exited successfully for both the
Unit and Engineering solution graphs. The known workspace-load notice did not correspond to a
format or analyzer difference.

The canonical packaged developer-journey gate passed without updating the committed contract:

- 30 freshly packed ViciOne packages;
- 18 executable developer-journey scenarios;
- 3 isolated provider-testing consumers;
- 29 runtime package APIs matching the committed baseline;
- 21,604 public API lines;
- SHA-256 `d3d7559c583bbbfdb99bd414ba67d4592ef12552e46fca9cef86f2f66b37c100`.

All three edited requirement projections parse as valid JSON.

## Mutation effectiveness

Twenty deliberate mutations were introduced one at a time, executed against a focused test, killed,
and then restored. Static final-state checks confirmed the intended implementation after restoration.

The first thirteen probes covered:

1. stale readiness generation overwriting the latest pending source;
2. loss of exact cancellation-token identity during task transfer;
3. accepting a child after supervisor stopping began;
4. skipping disposal of a pending active handle;
5. using caller cancellation for mandatory cleanup;
6. failing to fault context acquisition after runtime failure;
7. failing to cancel pending acquisition on handle disposal;
8. converting a runtime fault into cancellation;
9. sequential cleanup allowing borrowed failure to suppress owned cleanup;
10. accepting a null factory result indirectly;
11. suppressing creation-outcome publication failure;
12. sharing only completed stop tasks and executing duplicate concurrent stops;
13. retaining a failed stop task and preventing retry.

The seven post-Red-Team probes covered:

14. leaking a late context rejected by `AsyncPipeContextAgent`;
15. leaking a late context rejected by `AsyncPipeContextHandle`;
16. permanently caching a failed `PipeContextAgent` disposal attempt;
17. allowing a throwing `Stopping` callback to prevent resource release;
18. allowing a throwing `Stopped` callback to poison a successful stop;
19. excluding a faulted child completion from supervisor shutdown;
20. failing to observe each direction of rejected readiness/completion source faults.

Every mutation produced the expected failing assertion and nonzero test exit code. No mutation is
present in the final source.

## Internal adversarial review

The first read-only internal Red Team pass found three high-severity blockers and two medium-severity
gaps: late-context leakage, broken pipe-agent retry, lifecycle callback poisoning, swallowed child
completion, and unobserved rejected source tasks. All were corrected and directly tested.

A second read-only pass verified the corrected snapshot and reported no remaining commit blocker in
the reviewed lifecycle scope. It specifically rechecked retry races, terminal child retention,
callback behavior, late ownership transfer, and task ownership in `SupervisorExtensions`. This was
internal adversarial assistance and is not represented as independent external acceptance.

## Coverage statement

No fresh whole-product coverage percentage is claimed for this iteration. The ordinary MTP test
projects do not currently reference a compatible coverage provider; direct `--coverage` attempts on
the Abstractions test project discovered zero tests and exited with code 5. Those attempts were
rejected as evidence, and product dependencies were not changed merely to manufacture a number.

The last accepted whole-product baseline from the 2026-09-05 deep review remains:

- line coverage: 56,649 / 80,825 = 70.1%;
- branch coverage: 15,412 / 27,723 = 55.6%.

For this iteration, direct lifecycle tests plus the twenty killed mutations provide the current
effectiveness evidence. They do not imply 100% coverage of the complete product.

## Repository boundaries

`review/` was not edited. Generated `TestResults/` content was not staged. The iteration stages only
the reviewed source/API changes, their direct and architectural tests, requirement projections, and
this evidence.
