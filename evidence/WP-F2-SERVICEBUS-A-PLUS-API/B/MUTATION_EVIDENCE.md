# Work package B verified counterexamples

Each row is a deliberate, temporary source change compiled into a separate artifact tree below
`/private/tmp`. The named test was then executed in isolation and observed failing. The product source
was restored immediately after each run; the accepted Release artifacts and three complete profile
runs were not replaced by a mutant build.

This is targeted pseudo-mutation evidence for the highest-risk corrections, not a claimed global
mutation score or independent certification.

| Counterexample | Owning test and observed failure |
|---|---|
| Order SQLite quarantine rows only by identifier, ignoring creation time | `EntityFrameworkOutboxOperationsTests.GetQuarantined_ReturnsOnlyTheOwnedBusInDeterministicBoundedOrderAsync`; expected IDs `[cccc…, aaaa…]`, observed `[aaaa…, bbbb…]` |
| Replace the caller token passed to `ServiceBusProcessor.StartProcessingAsync` with `CancellationToken.None` | `ServiceBusConnectionContextTests.QueueClientStart_ForwardsTheCallerCancellationTokenAsync`; exact token equality failed |
| Encode an AMQP timestamp in Unix milliseconds instead of Unix seconds | `AmqpTimestampExtensionsTests.TimestampAtOrAfterEpoch_UsesUnixSeconds`; expected `1788518096`, observed `1788518096000` |
| Evaluate a DST transition from UTC clock fields instead of local wall-clock fields | `CronExpressionDaylightSavingTests.SpringGap_CarriesTheMissingOccurrenceIntoTheNextLocalHour`; expected `07:15Z`, observed `06:15Z` |
| Strip the offset before registering the one-shot job schedule | `InMemoryJobServiceTests.OneShotJob_RunsAtTheProviderOwnedScheduledInstantAndThenCompletesAsync`; state advanced prematurely from `WaitingForSlot` to `AllocatingJobSlot` |

Raw runner output is retained in the five `mutation-*.log` files in this directory. Every runner used
`--minimum-expected-tests`, strict zero-test handling, skip failure, and serial execution. Each process
returned exit code 2 because its intended counterexample was detected.
