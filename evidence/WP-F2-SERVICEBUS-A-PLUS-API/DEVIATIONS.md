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
