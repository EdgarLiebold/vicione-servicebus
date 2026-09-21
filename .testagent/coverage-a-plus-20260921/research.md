# ServiceBus A+ coverage campaign — research

Snapshot baseline: branch `feature/servicebus-a-plus-api`, commit
`e0cf987c845154fea81ec27b63592a910aceac37`.

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
- 32/32 loadable product assemblies observed in 36 fresh Cobertura reports.
- Line coverage: 79,569/90,165 = 88.2482%.
- Branch coverage: 79.2182–86.0351%. The interval is
  required because Cobertura does not identify branch arcs across reports.
- CRAP: 142 methods exceed 30.
- Static Microsoft Roslyn pairing heuristic: 4,287 source files, 1,416 test
  files, 2,379 paired and 1,908 unpaired. This is targeting information only,
  not coverage evidence.

## ActiveMQ phase result

- 21 new behavior tests received Microsoft `grade-tests` A ratings.
- 174/174 ActiveMQ Unit/Contract tests and 9,788/9,788 complete Unit/Architecture tests pass.
- Three canonical broker cases pass across OpenWire, Classic AMQP, and Artemis AMQP.
- All eight selected ActiveMQ baseline hotspots are now below CRAP 30.
- Two real defects were found while hardening provider fidelity: non-native `IFormattable` values
  could fail provider serialization, and `byte[]` was legal in a generic primitive map but forbidden
  in OpenWire message properties.
- Final adversarial review: PASS, no findings.

## Baseline risk inventory

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
