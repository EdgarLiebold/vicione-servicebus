# Observability

ViciOne.ServiceBus exposes one OpenTelemetry surface through the .NET diagnostics APIs. The service
bus creates signals; the application configures collection, sampling and exporters.

## Activation

Dependency-injection entry points register the standard .NET metrics services idempotently. Each
service provider owns its `IMeterFactory` scope, and ViciOne.ServiceBus never replaces a factory
registered by the application. Multiple buses in one provider share that provider's scope; separate
providers remain isolated.

Configurations built without dependency injection activate process-scoped instrumentation
explicitly:

```csharp
IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
{
    configuration.UseInstrumentation();
});
```

Both the `Meter` and `ActivitySource` name are `ViciOne.ServiceBus`. Their version is the product
assembly version. OpenTelemetry configuration selects that name, for example with
`AddMeter("ViciOne.ServiceBus")` and `AddSource("ViciOne.ServiceBus")`.

## Metrics

| Instrument | Type | Unit | Meaning |
|---|---|---:|---|
| `messaging.client.sent.messages` | Counter | `{message}` | Producer send attempts, including failed attempts |
| `messaging.client.consumed.messages` | Counter | `{message}` | Messages delivered to the receive pipeline |
| `messaging.client.operation.duration` | Histogram | `s` | Client send or receive operation duration |
| `messaging.process.duration` | Histogram | `s` | Consumer, handler, saga or activity processing duration |
| `vicione.servicebus.messaging.operations.active` | UpDownCounter | `{operation}` | Messaging operations currently executing |
| `vicione.servicebus.messaging.retry.attempts` | Counter | `{attempt}` | Retried processing attempts |
| `vicione.servicebus.messaging.delivery.duration` | Histogram | `s` | Time from the message sent timestamp to processing |
| `vicione.servicebus.outbox.messages` | Counter | `{message}` | Outbox enqueue and delivery outcomes |
| `vicione.servicebus.payload.admission` | Counter | `{decision}` | Serialized payload admission decisions |
| `vicione.servicebus.payload.body.size` | Histogram | `By` | Serialized application-body size |
| `vicione.servicebus.payload.envelope.size` | Histogram | `By` | Final transport-envelope size |
| `vicione.servicebus.durable_sender.admission` | Counter | `{outcome}` | Durable outbox admission outcomes |
| `vicione.servicebus.durable_sender.admission.size` | Histogram | `By` | Retained content size admitted to the outbox |
| `vicione.servicebus.durable_sender.delivery` | Counter | `{outcome}` | Carrier delivery-attempt outcomes |
| `vicione.servicebus.durable_sender.delivery.duration` | Histogram | `s` | Carrier delivery-attempt duration |
| `vicione.servicebus.durable_sender.consumer_completion` | Counter | `{outcome}` | In-memory consumer-completion outcomes |
| `vicione.servicebus.durable_sender.consumer_completion.duration` | Histogram | `s` | In-memory consumer-completion duration |
| `vicione.servicebus.reliability.abandoned` | Counter | `{decision}` | Explicit retained-abandon decisions |
| `vicione.servicebus.durable_sender.stored` | ObservableGauge | `{message}` | Retained outbox records |
| `vicione.servicebus.durable_sender.stored.content.size` | ObservableGauge | `By` | Retained outbox content size |
| `vicione.servicebus.durable_sender.pending` | ObservableGauge | `{message}` | Pending outbox records |
| `vicione.servicebus.durable_sender.retry_scheduled` | ObservableGauge | `{message}` | Outbox records awaiting a retry |
| `vicione.servicebus.durable_sender.awaiting_consumer_completion` | ObservableGauge | `{message}` | In-memory records awaiting consumption |
| `vicione.servicebus.durable_sender.quarantined` | ObservableGauge | `{message}` | Quarantined outbox records |
| `vicione.servicebus.durable_sender.oldest_pending.age` | ObservableGauge | `s` | Age of the oldest pending outbox record |

All messaging duration histograms publish the OpenTelemetry-recommended explicit bucket advice:
`0.005`, `0.01`, `0.025`, `0.05`, `0.075`, `0.1`, `0.25`, `0.5`, `0.75`, `1`, `2.5`, `5`, `7.5`
and `10` seconds. Exporters remain free to apply their own aggregation policy.

The standard messaging attributes are `messaging.system`, `messaging.operation.name`,
`messaging.operation.type` and, only for failures, `error.type`. Bounded ViciOne attributes are
`vicione.servicebus.processor.kind`, `vicione.servicebus.outbox.operation` and
`vicione.servicebus.outcome`.

Transport identities normalize to the OpenTelemetry registry value where one exists:
`activemq`, `aws_sqs`, `eventhubs`, `rabbitmq` and `servicebus`. ViciOne additionally uses the fixed
bounded values `in-memory`, `sql`, `other` and `unknown`.

Message types, endpoint addresses, payload data and application-defined tags are intentionally not
metric dimensions. This prevents unbounded cardinality and accidental data disclosure. Detailed
message correlation remains the responsibility of tracing and structured logs.

## Reliability boundary

Telemetry is observational. Exceptions from an application-owned meter factory, meter listener or
activity listener never change message delivery, retry, outbox or circuit-breaker behavior. Failed
activation disables signals only for that activation; it cannot reuse another provider's scope.

In-memory and persistent outboxes report enqueue and delivery as separate outcomes. A later
delivery failure therefore never rewrites a successful enqueue. `MessageJournal` signals describe
the optional diagnostic journal only; they are not a second Suite audit log beside journald/syslog.

`AddViciOneReliableMessagingHealthCheck<TBus>()` reports store reachability, capacity, pending age,
retry pressure, and quarantine state without exposing message bodies, addresses, or identifiers.
Applications decide how health results are published and which conditions affect orchestration.

An explicit `AbandonAsync` decision retains the terminal inbox record, writes a structured log, and
increments `vicione.servicebus.reliability.abandoned`. Repeating an inapplicable operation returns a
typed non-applied disposition and does not emit a second counter event.

StatsD, Windows performance counters, custom counter factories and mutable tag-extension APIs are
not supported. Export through an OpenTelemetry-compatible exporter instead.
