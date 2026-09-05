# Program deviations

## Test-generation working state

The repository acceptance contract requires `.testagent/` to be absent after work package A. The
test-generation workflow normally stores broad-scope research, planning, and status files there.
To preserve both requirements, the complete pre-existing directory is moved without content changes
to `evidence/native-tests/obligation-maps/`, and new program working records are maintained inside the
new work-package evidence directories. No test obligation record is discarded.

## Independent review boundary

The Developer AI records reproducible commands, results, static analyses, and verified mutations.
It does not claim to be an independent Developer Red Team or the Lead Architect. Those read-only
roles must examine each frozen commit in a separate session before formal acceptance.

## Related defect: suppressed execution-context flow

While annotating the in-memory outbox, the deferred-method constructor was found to assume that
`ExecutionContext.Capture()` can never return null. A caller may legitimately suppress context flow,
in which case registration later failed instead of running the deferred method. The implementation
now schedules the method while flow remains suppressed when no context was captured. This preserves
the intended isolation from the drain caller's ambient state and avoids a null dereference. An
internal adversarial review then exposed that null also represented an already-run or disposed
method. The final implementation atomically claims each method once, atomically detaches its context,
and removes each drain snapshot from the collection before execution. Repeated or concurrent runs,
and runs after disposal, are no-ops.

Regression evidence:
`InMemoryOutboxCheckpointTests.SuppressedExecutionContext_StillExecutesTheDeferredMethod` registers
under `ExecutionContext.SuppressFlow()`, drains twice under a different `AsyncLocal` value, and proves
one execution without ambient-state leakage.
`InMemoryOutboxCheckpointTests.RepeatedDrain_ExecutesANormallyCapturedDeferredMethodOnce` protects the
normally captured path, and `InMemoryOutboxCheckpointTests.DisposedDeferredMethod_DoesNotExecute`
protects disposal before execution.

## Related defect: subsecond RabbitMQ stream maximum age

The RabbitMQ stream configurator converted `MaxAge` to whole seconds. Values below one second wrote a
null `x-max-age` queue argument, producing an invalid broker declaration. Such a duration is below the
broker's supported granularity, so the implementation now removes the queue argument completely.

Regression evidence:
`RabbitMqStreamConfigurationTests.MaxAgeBelowBrokerGranularity_RemovesAnExistingQueueArgument` first
sets a valid maximum age, replaces it with 999 milliseconds, and proves the generated queue arguments
contain no `x-max-age` entry.

## Application overload resolution after surface reduction

Removing runtime-object and pipe overloads from the application interfaces exposed an intentional C#
resolution change: an `object`-typed variable would otherwise bind the remaining generic application
method as `T = object`, losing its runtime message contract. All such framework and test call sites now
select `.Advanced()` explicitly before invoking runtime-type, pipe, callback, or initializer forms.
The capability and runtime type are preserved without reintroducing an ambiguous application overload.

Regression evidence includes the interface-message dispatch, in-memory send-endpoint, buffered-bus,
conversation-context, and container endpoint-routing tests. The final application inventory contains
three send shapes and two publish shapes.

## SQLite ordering for UTC `DateTimeOffset` outbox rows

SQLite rejects `ORDER BY` over an Entity Framework Core `DateTimeOffset` expression. Client-side sorting
would make the bounded quarantine operation read every matching row. The SQLite branch therefore orders
the library-owned, UTC-normalized canonical persisted representation and then the outbox identifier,
while providers with native `DateTimeOffset` ordering retain the typed expression. `Take` remains in the
database query.

The regression uses three owned quarantine rows with deliberately opposing time and identifier order,
plus pending and foreign-bus rows. A targeted counterexample that ordered only by identifier was killed.

## Mandatory external asynchronous names

Quartz 4.0.0 requires explicit implementations named `IJob.Execute`, `IJobFactory.CreateJob`, and
`IJobFactory.ReturnJob`. Renaming these members would violate the external interface. They remain the
only non-entry-point task-like names without an `Async` suffix found by the bidirectional Roslyn check.
They are explicit framework contracts, not ViciOne public API choices.

## Related defect: nondeterministic saga concurrency test barrier

The existing held-instance concurrency test only waited until a cancel message was enqueued. Under a
loaded full-suite run, the held completion could be released before the cancel dispatch had entered the
saga repository. The completed saga was then removed and the later cancel was correctly discarded as a
missing-instance message, but the test incorrectly timed out waiting for it to appear as saga-consumed.

The test now wraps the in-memory repository with a test-only signaling decorator. It waits until the
exact cancel repository task is observable and asserts that this task is still pending before it sends
the unrelated-instance message or releases the held completion. This is a causal task-state barrier;
no timeout increase or production behavior change was used. The corrected test passed ten isolated
runs and all three final full-suite runs.

## Related defect: durable intent retired after failed or unconsumed delivery

Replacing the durable-send integration test's early consumer-fault signal with the terminal
`IReceiveObserver.PostReceiveAsync` boundary made a production defect deterministic. The in-memory
durable-send completion filter treated receive-pipeline completion as successful consumer delivery and
removed the stored intent even when the context was faulted or no consumer accepted the message.

The completion filter now retires an intent only when `IsDelivered` is true and `IsFaulted` is false.
Separate regressions preserve the intent after consumer failure and after an unconsumed/dead-lettered
message. The hardened class failed six of six times before the correction and passed three of three in
six repeated executions after it, followed by all three complete UnitArchitecture runs. The test uses
a causal receive-terminal barrier; no polling delay or timeout increase is involved.

## Payload admission and trace-envelope ordering

The existing architecture oracle required payload admission to run before creation of the send
activity. Admission materializes the serialized body, while send-activity creation writes the W3C
trace parent, trace state, and baggage headers that must be included in that body. Reversing the order
therefore removed propagation data and also prevented failed serialization attempts from producing
their required failure metric. Four independent telemetry regressions exposed the contradiction.

The enforced order is now: activity/header creation, payload admission, send observers, provider I/O.
This retains the security boundary required by the review—no observer or provider sees an unadmitted
message—while preserving trace propagation and fault observation. The architecture test asserts all
four boundaries in this causal order.

## Batching partition oracle under scheduler load

The batch integration matrix published six messages concurrently and assumed a 50-millisecond
from-first timer could not run until five had reached the collector. That is not a batching contract:
if only two messages arrive before the configured boundary, closing a two-message time batch is
correct. A complete-profile run exposed the permitted `2 + 4` partition while preserving every
message exactly once.

The size-and-tail cases now publish five messages, observe the exact five-message size completion,
then publish the tail. Their timer is reset from the latest arrival and uses a one-second semantic
limit; the general test timeout is unchanged. Direct fake-time tests continue to prove the exact
from-first and from-last timer boundaries without wall-clock assumptions. The revised seven-case
theory passed ten consecutive focused runs before the complete profile was repeated.

## Diagnostics cancellation continuation oracle

The diagnostics test for a stop that raises its budget cancellation assumed two `Task.Yield()` calls
were sufficient for the `Task.Delay` cancellation continuation to run. That scheduler assumption
failed both under full-profile load and in isolation, while `QuiesceAsync` still returned the required
`false` once awaited. The test now awaits that result through a one-second hang guard and propagates
the xUnit cancellation token. This preserves both assertions—budget cancellation is not quiescence,
and the operation must terminate—without weakening the product contract.

## Related defect: ActiveMQ shared session published before fault-listener registration

`ScopeSessionContextFactory` completed and exposed a shared send session before a separately scheduled
continuation registered the underlying connection's exception listener. A connection failure in that
window could leave a retained send endpoint holding a stale session after the receive-side connection
had already faulted. Repeated complete-profile runs exposed the ordering defect as a null listener at
the first usable-session boundary.

The listener and its completion cleanup are now installed inside the agent factory, before
`CreatedAsync` publishes the context. The lifecycle test therefore proves both cache reuse and the
precondition that fault observation exists before first use, then proves listener removal and recovery
to a different shared session. The corrected regression passed 50 consecutive isolated process runs
before the complete profile was restarted.
