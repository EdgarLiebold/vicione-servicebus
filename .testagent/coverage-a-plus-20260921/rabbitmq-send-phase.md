# RabbitMQ send phase (2026-09-22)

## Product finding and correction

`RabbitMqSendTransportContext.SendAsync<T>` formatted a positive TTL with `F0` and truncated a
positive delay to a `long`. An interval shorter than one millisecond became `0` on the AMQP wire;
other fractional intervals could be shortened. Both values now round up with integer ticks, without
overflow near `TimeSpan.MaxValue`. The send path was split into validation, queue proof, property,
delay, and telemetry helpers while preserving the original order and failure handling.

## Behavioral tests

- `SendAsync_RoundsPositiveTimeToLiveUpToTheNextWireMillisecondAsync` checks one tick, half a
  millisecond, a fractional millisecond, an exact millisecond value, and a near-maximum interval.
- `SendAsync_RoundsPositiveDelayUpToTheNextWireMillisecondAsync` checks the same critical rounding
  boundaries in the delayed exchange and AMQP `x-delay` header.
- `SendAsync_DirectReplyToUsesTheDefaultExchangeAndPreservesTheReplyRoutingKeyAsync` checks the
  actual publish frame and absence of delayed topology.
- `SendAsync_RecordsRoutingKeyOnlyWhenTheActivityHasAConcreteRouteAsync` checks the emitted
  telemetry tag on a real send.
- `SendAsync_EnforcesMandatoryRoutingFromADistinctPublishPayloadAsync` uses the public payload
  override seam and checks mandatory routing in the frame. Removing the payload projection caused
  this test to fail before it was restored.
- `DurableAcceptance_RejectsATransportExchangeThatDiffersFromTheValidatedDestinationAsync`
  checks rejection before topology work or publication.
- `DurableAcceptance_CanceledQueueProofDoesNotPublishOrEvictValidTopologyAsync` and
  `DurableAcceptance_CanceledPublishDoesNotMarkAcceptanceOrEvictValidTopologyAsync` cancel the
  exact context token during the broker operation and check acceptance, publication, and reuse of
  valid topology on the retry.

The initial focused run against the old product implementation had 8 failures in the TTL and delay
rounding cases. After the fix, the focused class passed 42/42 and the full RabbitMQ Unit project
passed 340/340 with Microsoft CodeCoverage and the repository's canonical
`tools/ci/coverage.settings.xml`, both with zero skipped tests. The local report is
`artifacts/coverage-a-plus-20260922-rabbitmq-send/rabbitmq-unit-canonical.cobertura.xml`.
The Release Unit/Architecture solution build passed with zero warnings and errors. After the
architecture test was updated, the complete gate passed 10,047/10,047 with zero skips. The isolated
RabbitMQ LocalIntegration solution also built with zero warnings and errors. The canonical fixture
runner passed 31/31 tests against a fresh RabbitMQ broker with the same coverage settings; its
`fixture-findings.json` has an empty findings list. The local broker report is
`artifacts/coverage-a-plus-20260922-rabbitmq-send/rabbitmq-local-canonical.cobertura.xml`.

| Method in RabbitMQ send source | Lines | Reported branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| `SendAsync<T>` state machine | 34/34 | 18/20 | 20 / 20 |
| `ValidateTransportAcceptance<T>` | 18/18 | 12/14 | 14 / 14 |
| `HasValidatedRouteAndDelivery<T>` | 7/7 | 12/12 | 12 / 12 |
| `VerifyBeforePublishAsync<T>` state machine | 12/12 | 7/8 | 8 / 8 |
| `ApplyBasicProperties<T>` | 17/17 | 18/20 | 20 / 20 |
| `ConfigureDelayedExchangeAsync<T>` state machine | 6/6 | 6/6 | 6 / 6 |
| `RoundPositiveDurationUpToMilliseconds` | 3/3 | 0/0 | 1 / 1 |
| `TagRoutingKey` | 3/3 | 6/6 | 6 / 6 |

CRAP uses Cobertura complexity and method line coverage. The remaining instrumented branch gaps
include the invalid send-context type, absent destination-address fallback in durable diagnostics,
null content type, and request/reply permutations. These are visible gaps, not a claim of complete
branch coverage. An architecture test that assumed preflight logic remained textually inside
`SendAsync<T>` was updated to verify the helper call before topology and the validation conditions
inside the helper. Independent read-only adversarial review returned PASS on the product and
RabbitMQ test diff and separately on the architecture-test adjustment. The added sub-millisecond
cases assert frames through a fake channel; the existing broker suite proves the broader real
send/acceptance path, not these new exact sub-millisecond values at a broker. The complete 36-report
product-wide profile at `a95505227` remains the latest aggregate, so global A+ is open.
