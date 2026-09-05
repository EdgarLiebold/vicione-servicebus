# Work package D status

Status: complete

Completed:

- 40/40 actual concrete options models are semantically inventoried, positively tested, and bound to
  negative invariant tests where applicable; the review's lexical baseline of 44 is reconciled.
- One per-bus startup validator aggregates the complete composition before runtime services use it.
- Contracts, redaction, payload admission, reliable messaging, and message-journal selection belong to
  the owning bus block.
- `UseMessageJournal` supports the core builder plus EF Core and Azure Table provider selection.
- `MessageLimits` is mandatory and enforced across all nine receive paths, JSON depth, raw JSON, and
  `TryGetMessage`.
- Legacy CBC/V1/V2 message-data encryption is removed; one versioned, authenticated AES-GCM path with
  rotating keys remains.
- All 276 semantic `ConfigurationException` creations use the required actionable one-line form.
- The formatter, three strict builds, package build, Developer Journeys, vulnerability audit, and
  three complete 3,676-test UnitArchitecture runs are green with zero skips.
- Acceptance exposed and corrected an ActiveMQ shared-session listener race and two invalid timing
  assumptions in existing tests; all are documented with focused stability evidence.
- Azure Table provider wiring is unit-tested without cloud I/O; no real-cloud result is claimed.
- `review/**` remains untouched.
