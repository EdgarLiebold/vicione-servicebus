# Work package A verified counterexamples

These were deliberate, temporary source changes made by the Developer AI against the focused Release
test binaries. Each counterexample was observed failing, then reverted. A clean rebuild and all final
acceptance runs followed. This is targeted pseudo-mutation evidence, not an independent mutation-score
certification.

The focused runner shape was:

```text
artifacts/sdk/bin/<test-project>/release/<test-executable> \
  --filter-method <fully-qualified-method> --minimum-expected-tests 1 \
  --zero-tests-policy strict --fail-skips on --parallel none
```

| Counterexample | Owning test and observed failure |
|---|---|
| Remove the pending-method snapshot clear | `InMemoryOutboxCheckpointTests.RepeatedDrain_ExecutesANormallyCapturedDeferredMethodOnce`; pending count expected 0, actual 1 |
| Skip a deferred method when `ExecutionContext.Capture()` returned null | `InMemoryOutboxCheckpointTests.SuppressedExecutionContext_StillExecutesTheDeferredMethod`; observation was `not-invoked` |
| Remove the deferred method's atomic one-shot guard | `InMemoryOutboxCheckpointTests.DisposedDeferredMethod_DoesNotExecute`; invocation count expected 0, actual 1 |
| Retain an `x-max-age` entry with a null value for subsecond stream age | `RabbitMqStreamConfigurationTests.MaxAgeBelowBrokerGranularity_RemovesAnExistingQueueArgument`; bus construction failed on the null queue argument |
| Copy wire headers into a default-comparer dictionary instead of retaining the supplied dictionary | `SystemTextJsonMessageBodyTests.WireHeaders_PreserveComparerNullEntriesAndLiveUpdates`; case-insensitive lookup failed |

The assertions observe externally meaningful state: execution count and ambient-context value, pending
checkpoint count, disposal behavior, broker argument presence, dictionary comparer behavior, null-value
presence, source-dictionary liveness, and exact failure boundaries. None relies only on the absence of an
exception.
