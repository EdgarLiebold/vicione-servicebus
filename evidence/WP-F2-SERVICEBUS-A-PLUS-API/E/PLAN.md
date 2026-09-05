# Work package E plan

1. Define one per-bus reliable-messaging model for transactional outbox admission, inbox ownership,
   scheduled delivery, quarantine operations, health, telemetry, and one delivery loop.
2. Map the in-memory and Entity Framework paths onto the same store contracts and lifecycle while
   retaining application-owned commit boundaries, typed multi-bus isolation, bounded capacity, leases,
   retry state, and terminal quarantine.
3. Make scheduling an explicit adapter choice under `UseReliableMessaging`, retain no implicit fallback,
   and allow a transport adapter to report success only at a proven durable-acceptance boundary.
4. Migrate Journeys 06, 07, and 11; provide SQLite and PostgreSQL migration scripts; bind restart,
   cancellation, duplicate, due-time, pagination, abandonment, inbox-retry, and broker-outage behavior
   with executing tests and one-cause mutations.
5. Run locked restore, three strict Release builds, both format gates, three identical unfiltered
   UnitArchitecture profiles, package-only journeys, real RabbitMQ and PostgreSQL acceptance,
   vulnerability inventory, API inventory, static gates, and an explicitly internal adversarial review.
