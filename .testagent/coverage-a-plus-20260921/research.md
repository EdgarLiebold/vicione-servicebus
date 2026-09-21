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

## Generic SQL topology slice result

- 9 new behavior tests received Microsoft `grade-tests` A ratings.
- 147/147 SQL Unit/Contract tests and 9,797/9,797 complete Unit/Architecture tests pass.
- The six selected generic SQL topology hotspots fell from CRAP 72–272 to CRAP 8–16, except the
  namespace scanner at CRAP 12.11; all are below 30.
- Logical subscription equality, queue delivery-limit diagnostics, public null validation, atomic
  explicit-type validation, both scan branches, invalid contract rejection, and namespace
  boundaries have exact positive and negative controls.
- Final adversarial review: PASS, no findings after its product and oracle corrections.

## SQL host-configuration slice result

- 16 new behavior test methods received Microsoft `grade-tests` A ratings.
- 190/190 SQL Unit/Contract tests and 9,840/9,840 complete Unit/Architecture tests pass.
- `ConfigurationSqlHostSettings.Validate` fell from CRAP 49.85 to helpers at CRAP 18 and 16;
  `PostgreSqlHostSettings.ParseHost` fell from CRAP 203.47 to CRAP 14 with parser helpers at 12 or
  below. Their selected validation and parsing paths have 100% line and branch coverage.
- Tests prove complete credential suffixes, validation boundaries, supported and malformed host
  shapes, atomic replacement, actual Npgsql target selection, provider-option preservation, inline
  default-port precedence, and explicit collapse of multi-host state.
- Final adversarial review and the separate post-refactoring review: PASS, no findings.

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
