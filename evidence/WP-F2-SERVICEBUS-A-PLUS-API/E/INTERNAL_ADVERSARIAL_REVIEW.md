# Internal adversarial review

This review was performed and coordinated by the implementing Developer AI. It is internal adversarial
evidence only. It is not an independent Developer Red Team, external review, or architecture acceptance.

## Challenges applied

- Searched the source tree for a second durable delivery loop, a second quarantine facade, retired
  outbox entry points, implicit scheduler selection, and a provider that could claim acceptance without
  a terminal transport boundary.
- Challenged in-memory and EF behavior at exact capacity, exact due time, restart, duplicate identity,
  reused identity with changed schedule, active lease, retry exhaustion, terminal quarantine, and
  operator idempotency boundaries.
- Challenged RabbitMQ at persistent storage, mandatory return, publisher confirmation, cancellation,
  sender shutdown, and real broker delivery.
- Challenged configuration by omitting every required reliable-messaging policy and by selecting zero
  retention, duplicate owners, missing owners, and ambiguous transport/store registrations.
- Challenged migration scripts with retained retry/quarantine/inbox/capacity data and a second execution
  against the already migrated database.
- Recomputed the compiled API inventory and repeated the complete UnitArchitecture profile three times
  to expose timing or shared-state instability.

## Findings closed during review

- Existing idempotency tests did not vary `DueAt` even though the product correctly treated it as
  immutable. In-memory and EF assertions now bind that field.
- Existing schedule tests did not attempt cancellation after a lease was acquired. Both stores now have
  an executing lease-fence assertion.
- RabbitMQ real tests bound the broker behavior, but no fast architecture gate protected the three
  required send flags. The new gate binds persistent, mandatory, and confirmed publication plus the
  awaited acceptance ordering.
- Required policy tests omitted missing store limits, missing delivery, missing retention, and the exact
  zero-retention boundary. All now fail at startup.
- Four older RabbitMQ fault-redrive tests had not adopted mandatory `MessageLimits`; the real full profile
  exposed this before broker I/O. Their shared setup now declares limits, and all 27 tests pass.
- The file-based public-API generator inherited NativeAOT and therefore locked SDK-patch-specific
  ILCompiler packages after SDK pinning was removed. NativeAOT is now disabled for this reflection tool;
  its lock graph contains no implicit compiler/runtime package and the full journey gate passes.

## Residual boundary

RabbitMQ is the only transport in this package that proves the required durable-acceptance boundary.
Other transports remain explicitly unsupported for this feature and fail at startup; no support is
inferred from their ordinary send capability. SQLite and PostgreSQL migrations executed locally; no
unrequested cloud-provider acceptance is claimed. Formal independent review remains a separate PO/Lead
step on the frozen commit.
