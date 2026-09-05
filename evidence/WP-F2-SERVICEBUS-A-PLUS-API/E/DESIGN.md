# Work Package E — Reliable Messaging Design

## Contract and ownership decision

`ReliableMessaging` is a per-bus capability. It owns producer outbox, consumer inbox, scheduled
delivery, quarantine, health and delivery execution as one composition. Applications select exactly
one store and may explicitly select one scheduler adapter. The transport contributes an acceptance
dispatcher only when it can prove the terminal acceptance required by the capability matrix.

| Concern | Public contract | Owner | Cardinality per bus |
|---|---|---|---|
| Application configuration | `UseReliableMessaging(Action<IReliableMessagingConfigurator<TBus>>)` | owning bus block | one |
| Producer admission | `IDurableSender<TBus>` | reliable outbox | one scoped facade |
| Outbox persistence | `IOutboxStore<TBus>` | selected store provider | one |
| Inbox persistence | `IInboxStore<TBus>` | selected store provider | one |
| Schedule persistence | `IScheduleStore<TBus>` | selected store provider | one |
| Durable transport acceptance | `IDurableSendDispatcher<TBus>` | transport adapter | one supported adapter |
| Delivery execution | `ReliableMessagingDeliveryService<TBus>` | core | one `IHostedService` type |
| Operations | `IReliableMessagingOperations<TBus>` | core facade | one API |
| Health and telemetry | existing ViciOne schema | core facade and delivery service | one instrument per bus |

The existing durable-send identifiers, receipts, serialized envelope, leases, pagination and typed
operation results remain the outbox vocabulary. `IOutboxStore<TBus>` replaces the former durable-only
store name. It has the same fenced admission/delivery semantics and additionally preserves `DueAt`.
The inbox receives an equivalent key, state, lease, retry, quarantine and pagination vocabulary.
`IScheduleStore<TBus>` is not a second queue: it admits a serialized outbox intent with `DueAt` and
supports cancellation. Therefore scheduled messages share capacity, fencing, retry and quarantine
with ordinary outbox messages.

## Persistent model

| Table | Key and identity | Principal states | Capacity / retention |
|---|---|---|---|
| `vicione_outbox` | `(bus_key, id)`, immutable generation token | `Pending`, `RetryScheduled`, `AwaitingConsumerCompletion`, `Quarantined` | count and payload bytes are admitted atomically; quarantine retains capacity |
| `vicione_reliable_capacity` | `bus_key` | current retained count/bytes | hard ledger used in the same transaction as admission/removal |
| `vicione_inbox` | `(bus_key, message_id, consumer_id)` | `Processing`, `Consumed`, `RetryScheduled`, `Quarantined`, `Abandoned` | duplicate key is the exactly-once processing fence; terminal rows follow retention |
| `vicione_recurring_schedule` | `(bus_key, schedule_id)` | `Active`, `Paused`, `Cancelled`, `Quarantined` | each occurrence materializes one idempotent outbox row |

### Outbox state machine

| From | Trigger | To | Durable effect |
|---|---|---|---|
| none | commit admission | `Pending` | immutable intent and capacity ledger commit together |
| `Pending` / `RetryScheduled` | due claim | same state, leased | lease token and expiry fence one owner |
| leased | confirmed transport acceptance | removed | capacity released atomically |
| leased | in-memory dispatch | `AwaitingConsumerCompletion` | replay remains possible until logical consumer completion |
| leased | transient failure | `RetryScheduled` | bounded exponential `NextAttemptAt` |
| leased | permanent/invariant/attempt limit | `Quarantined` | retained for explicit operator decision |
| `Quarantined` | requeue / discard / abandon | `Pending` / removed / `Abandoned` | typed result; every action is explicit and idempotent |

### Inbox state machine

| From | Trigger | To | Meaning |
|---|---|---|---|
| none | receive and acquire | `Processing` | message/consumer identity is fenced before user code |
| `Processing` | transaction commits | `Consumed` | user state and all produced outbox rows commit together |
| `Processing` | durable retry requested | `RetryScheduled` | retry is persisted with `DueAt`; no volatile timer owns it |
| `RetryScheduled` | due claim | `Processing` | a fenced attempt resumes processing |
| any active | permanent/invariant/attempt limit | `Quarantined` | retained and visible through the common operations API |
| `Quarantined` | abandon | `Abandoned` | terminal decision is retained and logged |
| `Consumed` | duplicate receive | `Consumed` | body is not executed; acknowledgement is repeated |

Cancellation before a persistence commit leaves no accepted intent. Cancellation after commit never
revokes the durable receipt: the committed row remains deliverable. Cancellation after transport
dispatch but before the terminal state transition intentionally permits replay (at-least-once), never
loss. Store mutations are generation- and lease-fenced so stale completions cannot delete a later
incarnation of the same id.

## Mapping of the three existing paths

| Existing path | New mapping | Removed ownership |
|---|---|---|
| Durable Sender | `IDurableSender<TBus>` admits directly to `IOutboxStore<TBus>`; types and receipts remain | durable-only configuration/store/operations names |
| EF bus outbox and consumer inbox | scoped `DbContext` stages `vicione_outbox` and `vicione_inbox` rows in the user transaction; the common service claims them | EF delivery loop, separate cleanup loop, blocking coordinator path |
| In-memory outbox/inbox middleware | `UseInMemoryStore()` selects volatile implementations of all three stores; endpoint middleware binds to the common inbox/outbox session | separate application-facing in-memory-outbox switch |
| Scheduling | a configured scheduler writes `DueAt` on an outbox row; recurring definitions live in the schedule store | implicit scheduler fallback and transport-specific default selection |

`context.Outgoing` and scoped `bus.SendAsync` use the EF session when a matching `DbContext` is active.
Outside a unit of work, `IDurableSender<TBus>` is the explicit durable entry point. Consumer-produced
messages are part of the inbox transaction. All four cases therefore converge on the same outbox row
and the same delivery service.

## Adapter inventory and fail-closed rules

| Adapter choice | Store | Scheduler | Durable acceptance |
|---|---|---|---|
| `UseEntityFramework<TDbContext>()` | SQLite, PostgreSQL or SQL Server | outbox `DueAt` plus recurring table | independent of transport |
| `UseInMemoryStore()` | process-local, test/development only | fake-time-capable in-memory due queue | consumer completion |
| `UseQuartzScheduler()` | selected reliable store remains authoritative | Quartz only materializes due outbox intents | no implicit fallback |
| `UseTransportScheduler()` | selected reliable store remains authoritative until adapter hand-off | explicitly selected native scheduling | only a transport-declared adapter |
| `UseInMemoryScheduler()` | selected reliable store | explicitly volatile timing adapter | tests only |
| RabbitMQ dispatcher | any selected store | optional transport adapter | publisher confirm + persistent + mandatory |
| In-memory dispatcher | in-memory or EF store | optional in-memory adapter | logical consumer completion |
| all other transports | any | none unless explicitly supported later | startup failure |

Missing store, multiple stores, multiple scheduler adapters, a scheduler with no store, or a transport
without a durable-acceptance adapter is a startup composition error. Scheduling without a reliable
store and without an explicit adapter fails with:

`Scheduling for bus 'X': no scheduler is configured. Configure UseReliableMessaging(...) or choose an adapter explicitly.`

## Migration and rollout

There is no runtime compatibility layer. Provider scripts in `docs/migrations/` perform an explicit,
reviewable data migration:

| Provider | Script | Migration |
|---|---|---|
| SQLite | `reliable-messaging-sqlite.sql` | creates canonical tables, copies pending/quarantined legacy outbox rows, records inbox terminal state, then leaves legacy tables for operator-controlled removal |
| PostgreSQL | `reliable-messaging-postgresql.sql` | same semantic mapping with transactional DDL and provider-native constraints/indexes |
| SQL Server | `reliable-messaging-sqlserver.sql` | same semantic mapping with guarded DDL and transactional copy |

The scripts never infer a bus identity. Operators must provide the documented bus key before the copy.
Legacy retry and quarantine fields map without resetting attempts. Rows that cannot be mapped to a
stable destination or message identity are rejected by the script instead of being silently dropped.
Deployment order is: stop writers, back up, apply script, verify row/count/byte reconciliation, deploy
the new binaries, start one instance, verify health and quarantine, then scale out. Rollback restores
the backup; the product does not dual-write old and new schemas.
