# Developer journeys package-consumer gate

These eighteen source examples are compiled as one `net10.0` class library exclusively against
freshly packed ViciOne ServiceBus NuGets. The project deliberately contains no `ProjectReference`.

Run `tools/ci/verify_developer_journeys.sh` from any directory. The script packs the
current source tree into an isolated temporary feed, restores this project in locked mode into an
isolated global-packages directory, and builds it with warnings as errors.

The scenarios cover minimal RabbitMQ setup, send, publish, request/response, consumer retry,
transactional outbox, typed durable send, MessageData, MultiBus, partitioned consumption,
scheduling, health/telemetry, the unit-test harness, Azure Service Bus integration setup, a bounded
message journal, explicit message limits, duplicate-safe inbox processing, and the minimal Suite
package composition.
