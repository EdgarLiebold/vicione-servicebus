# V5 API usability review disposition

Date: 2026-09-04

The protected review was treated as adversarial input, not as an executable donor or an assumed description of the
merged product. Every finding and every previously unverifiable scenario was reconciled against the current source,
package graph, native tests, and freshly packed assemblies.

## Confirmed findings

| Review item | Disposition |
|---|---|
| API-001: persistence envelope exposed as application API | Closed. `IDurableSender<TBus>.SendAsync<T>` is the application boundary and creates the serialized durable intent through the real send-context, routing, contract-identity, serializer, `MessageData`, header, and payload-admission mechanisms. The serialized store contract remains provider SPI. |
| API-002: quarantine cannot be enumerated completely | Closed. The operator API uses bounded seek pagination with a versioned opaque URL-safe continuation token, deterministic `QuarantinedAt` descending / id ascending ordering, and explicit invalid-token rejection. Native and EF owners cover more than 1,000 records, equal-time ties, concurrent changes, and restart continuity. |
| API-003: fragmented durable-sender composition | Closed. `UseDurableSender` owns the coherent application flow, provider selection, options, and startup validation. Duplicate or missing composition fails deterministically. |
| API-004: application API and provider SPI insufficiently separated | Closed additively. Provider-facing durable contracts remain available for implementations but are marked `EditorBrowsableState.Never`; application journeys use only the typed facade and builder. |
| API-005: incomplete startup validation | Closed. Relevant options use `ValidateOnStart`, and a hosted composition validator rejects missing, duplicate, or incompatible registrations before message traffic begins. |
| API-006: feature-local persisted MultiBus identities | Closed. `BusPersistenceIdentity<TBus>` is the single deterministic, bounded identity used by both EF outbox and durable-send storage. |
| API-007: blocking-consumer analyzer too broad | Closed by verification and negative-corpus coverage. Diagnostics remain scoped to actual consume methods; administrative/helper methods do not trigger the rule. |
| API-008: operator methods return only `bool` | Closed. Requeue and discard expose typed operation results that distinguish success, missing ids, and invalid current state. |
| API-009: inconsistent generic constraint | Closed. The durable-sender application surface consistently constrains `TBus` as an `IBus` reference type. |
| API-010: contract-catalog source generator | No current correctness defect. Retained as a possible V6 compile-time optimization; the additive catalog remains deterministic, validated, and fully tested. |
| API-011: `IHostApplicationBuilder` convenience layer | No current correctness defect. Deferred to V6 to avoid introducing a second configuration path; `IServiceCollection` plus the typed ServiceBus builder remains canonical. |
| API-012: introduce a byte-size value object | Correctly identified by the reviewer as not a finding. Explicit byte-named limits are retained. |

## Previously unverifiable scenarios

| Review item | Reconciliation evidence |
|---|---|
| U01: partitioned-consumer registration | Present as a high-level typed consumer configuration and compiled in the package-only journey. Native tests verify selector and concurrency semantics. |
| U02: send/publish/request overload families | Present in the merged source and compiled from restored packages in separate send, publish, and request/response journeys. |
| U03: provider registration | Provider registration is source-bound and represented in the capability matrix. RabbitMQ and Azure Service Bus setup compile from freshly packed packages. |
| U04: scheduling and recurring scheduling | The Quartz scheduling surface is present and the scheduled-message journey compiles from the packed package. Existing native suites retain recurring-scheduling coverage. |
| U05: packaged testing surface | The unit-test-harness journey restores and compiles exclusively against the freshly packed Testing package. |
| U06: EF outbox and MultiBus | The package journey compiles the EF outbox flow; native EF tests verify shared per-bus identity and isolated registration. |
| U07: jobs, sagas, state machines, and routing slips | These product areas are present in the merged engineering graph and execute in the complete native Unit/Architecture regression. No V5.4 rewrite was required. |
| U08: provider capability semantics | Static capability ownership, configuration, and package usability are verified. Real broker/cloud crash and durability certification remains an explicit release-environment gate and is not represented as local evidence. |

## Developer-journey gate

Fourteen focused examples compile as a locked `net10.0` package consumer without any `ProjectReference`: minimal
RabbitMQ, send, publish, request/response, consumer retry, transactional outbox, typed durable send, `MessageData`,
MultiBus, partitioned consumption, scheduled send, health/telemetry, unit-test harness, and Azure Service Bus setup.
The gate packs eight current packages into an isolated feed before restoring the examples, so source references cannot
mask missing or wrongly packaged public API.

## Boundary retained honestly

No local Docker daemon or external cloud/broker environment was available for this closure. Provider runtime,
failover, and abrupt-loss certification therefore remains a release-environment obligation. This does not leave an
unresolved local API finding: it preserves the review's distinction between compile-time/package proof and real
provider durability proof.
