# ServiceBus A+ coverage campaign — research

Snapshot: branch `feature/servicebus-a-plus-api`, HEAD
`52944958115e719506f88c9f4bd5de1901bb6545`. The tracked `src`/`tests` binary-diff
SHA-256 is `4561c1ba5bcf115195b243f2e924b8df37fbbd1f99edf0feef22eea37afe0732`.

## User acceptance checklist

- `A+ für Line Coverage, Branch Coverage und CRAP`.
- `nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und Regressionen hart prüfen`.
- Use the applicable Microsoft .NET testing skills from `dotnet/skills`, including
  `code-testing-agent`, `coverage-analysis`, `find-untested-sources`,
  `test-gap-analysis`, `assertion-quality`, and `run-tests` in their defined scopes.
- Run adversarial read-only reviews and retain only tests with discriminating
  behavior assertions.

## Current measured universe

- .NET 10, Microsoft Testing Platform, xUnit v3.
- 32/32 loadable product assemblies observed in exactly 29 fresh Cobertura reports:
  16 Unit/Architecture reports plus all 13 local-provider projects.
- All current tests are green: 9,682 Unit/Architecture and 526 local-provider tests;
  provider runs have 0 failed, 0 skipped, and four empty fixture-finding sets.
- Line coverage: 78,405/90,135 = 86.9862%.
- Branch coverage: 27,761–30,094/36,272 = 76.5356–82.9676%. The interval is
  required because Cobertura does not identify branch arcs across reports.
- CRAP: 21,346 methods; 198 methods exceed 30; median 1, p95 10, p99 29.13,
  maximum 702.
- Static Microsoft Roslyn pairing heuristic: 4,287 source files, 1,416 test
  files, 2,379 paired and 1,908 unpaired. This is targeting information only,
  not coverage evidence.

## Risk inventory

The largest CRAP groups are RabbitMQ (33), Core (32), Amazon SQS (25), Azure
Service Bus (24), Abstractions (20), ActiveMQ (16), generic SQL transport (14),
Job Service (10), PostgreSQL (6), and SQL Server (5). The first RabbitMQ phase
targets real transport contracts with the highest risk and clear oracles:

- AMQP receive-header normalization and synthesized routing metadata.
- persisted RabbitMQ transport properties and missing-value behavior.
- diagnostic broker-topology projection, including null-valued arguments.
- declaration equality/hash contracts and field-specific negative controls.
- move-header overwrite/removal/UTF-8 behavior.
- send-setting topology and diagnostic projections.

Representative repository tests use xUnit facts/theories, exact values and
types, explicit exception assertions, `RequirementCoverage` bindings, no skips,
and no timing sleeps. New tests must follow those conventions.

## Quality risks to reject

- Calling a method without asserting a transport-visible result.
- Reflection-only coverage of private implementation details when a public or
  internal behavior surface exists.
- Assertions that accept multiple unrelated outcomes.
- Tests whose only purpose is executing a line, property, or `ToString` without
  verifying the diagnostic contract.
- Provider tests that silently pass with zero discovered tests or missing
  Cobertura output.
