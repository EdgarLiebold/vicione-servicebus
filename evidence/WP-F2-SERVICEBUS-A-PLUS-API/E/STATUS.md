# Work package E status

Status: complete

Completed:

- `UseReliableMessaging` is the single per-bus composition for outbox, inbox, scheduled messages,
  retention, quarantine, health, telemetry, and delivery execution.
- `IOutboxStore<TBus>`, `IInboxStore<TBus>`, and `IScheduleStore<TBus>` have in-memory and Entity
  Framework implementations with typed isolation, bounded capacity, idempotent intent, leases, retry,
  quarantine, and operator transitions.
- The transactional EF outbox contributes work to the one core delivery service; its former separate
  delivery loop has been removed.
- Exactly three explicit scheduler choices remain under the reliable-messaging builder:
  `UseTransportScheduler`, `UseQuartzScheduler`, and `UseInMemoryScheduler`. There is no implicit
  scheduler fallback.
- RabbitMQ reports transport acceptance only after a persistent, mandatory, publisher-confirmed send;
  unsupported transports fail composition instead of claiming durable acceptance.
- Journeys 06, 07, and 11 use the unified model. SQLite and PostgreSQL migration scripts preserve
  retained outbox, inbox, retry, quarantine, and capacity state without a compatibility runtime.
- The complete profile passed three times at 3,696/3,696 with zero skips. Real RabbitMQ passed 27/27,
  SQLite migration passed 3/3, and real PostgreSQL migration passed 1/1.
- Eleven one-cause mutations were killed. Four initially surviving gaps were closed with stronger
  assertions and then killed on repetition.
- Strict builds, format gates, packed Developer Journeys, API inventory, static ownership gates,
  vulnerability inventory, and diff whitespace checks are green.
- `review/**`, `LICENSE.txt`, `NOTICE`, `COPYRIGHT`, and the package-G `CHANGELIST.md` work remain
  outside this package's staged change set.
