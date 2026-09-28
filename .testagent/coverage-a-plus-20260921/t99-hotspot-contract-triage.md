# T99 — coherent hotspot contract triage

Baseline: T97 exact 33-profile aggregate at `3cb94a285`; current product
baseline after T98 is `c61ac4f20`. T99 has not changed product or test code.

## Reviewed source areas and test oracles

- `EndpointRecurringMessageScheduler` and `PublishRecurringMessageScheduler`:
  examined typed, untyped, runtime-contract and initialized send/publish paths,
  endpoint resolution, control commands and pipe adaptation. The existing
  `RecurringSchedulingCompletionTests.Scheduling_PreservesCommandAndWaitsForEndpointCompletionAsync`
  crosses two command transports, two payload routes, eleven forms and three
  asynchronous outcomes. It asserts command type and schedule identity,
  destination, payload contract, context token, pipe metadata, pending status
  and exact completion or fault. Their uncovered wrapper and validation lines
  alone do not justify new tests.
- `RetryConfigurationExtensions` and the immediate, interval, exponential and
  incremental policy constructors: checked configuration forwarding and
  timing validation. The constructors reject invalid limits and delays. No
  separate behavior defect is established by the uncovered extension lines.
- `PropertyProviderFactory`: checked task, nullable, message-data, variable,
  named-value, scalar, array, enumerable and dictionary conversion selection.
  Existing provider contract tests assert value conversion and unsupported
  routes across these families. A coverage-only test is not justified.
- `ReceiveEndpointDispatcher` and its typed decorator: checked dispatch
  ownership and cancellation against `ReceiveEndpointDispatcherTests`, which
  exercise cancellation during consumption, successor delivery, concurrent
  metrics, zero-activity signaling and raw/empty body delivery.

## Open question before product change

`InMemoryReliableInboxContextFactory` and its EF counterpart prefer an
explicit cancellable operation token over the consume-context token. The same
precedence occurs in their contexts and EF outbox code, while the older
in-memory outbox factory links tokens. This needs contract confirmation and
an in-flight behavioral test before any cross-provider change. A pre-canceled
token assertion alone would not prove the relevant lifetime behavior.

Independent read-only Red Team review: PASS, no verified concrete P1/P2 in
these areas. It classified the missing exception-filter detail in
`NoRetryPolicy.Probe` as a low-priority observability inconsistency, because
probe output has no established completeness contract. It also confirmed
that the two-token candidate is an unestablished contract choice rather than
an evidenced product defect. No T99 test or full profile was run because no
product or test code changed. T97 remains the latest global measurement.
This review does not count as a remediation packet in the 20–30 packet
measurement interval.
