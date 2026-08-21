# Execution plan — ViciOne.ServiceBus test reconstruction

Lead plan SHA-256 `6a4dd0237d574d264548d8a9d03c7a9d6f5ff6a11249354b58a8ff2d02e43776` — the sole target-architecture contract. This file is the team's execution
plan only: ownership, sequence, commands, mappings, blockers. It changes no architecture.

Corrected under Lead directive `DIR-A0071-R0-CORRECTION-01`. Frozen research result:
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0-CORRECTION-01/R0_FROZEN_RESULT.md`.

## 1. Exact commands

Measured, not quoted — see `R0/MTP_COMMAND_FORM_PROBE.md`.

| Purpose | Command |
|---|---|
| Profile run | `dotnet test --solution ViciOne.ServiceBus.Tests.<Profile>.slnx --minimum-expected-tests <N> --report-trx` |
| Focused cohort run | `dotnet test --project <target>.csproj --minimum-expected-tests <cohort N> --report-trx` |
| Evidence build | `dotnet build --no-incremental -bl:<evidence>/build.binlog -p:EvidenceProfile=true -p:RequireCompleteObligationSet=true` |
| Architecture rules | ordinary xUnit outcomes inside `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests` |
| Filters | `--filter-class` / `--filter-method` / `--filter-trait`; never `--filter "ClassName=…"` |
| Crash / hang | `--blame-crash`, `--blame-hang-timeout` |

MTP arguments pass **directly** — no `--` separator, no positional path. This needs the SDK 10 opt-in
in `global.json`, which the Lead adds to the write scope together with the R0 approval. It is not
edited in the correction commit.

## 2. Execution-owner matrix

41 cohorts, 3663 obligations, every executable target project in exactly one profile. Roles per
cohort in section 3.

| Cohort | Wave | Source owner | Inherited input | Target project | Profile | Obl. | Anchor |
|---|---|---|---|---|---|---:|---|
| `CO-ARCHITECTURE-TESTS` | F1 | `(repository-wide structure and graph rules)` | — (new work from gap analysis) | `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests` | `UnitArchitecture` | 20 | none |
| `CO-CORE` | C1 | `src/ViciOne.ServiceBus` | `tests/ViciOne.ServiceBus.Tests` | `tests2/ViciOne.ServiceBus.Tests` | `UnitArchitecture` | 1805 | core, entity-framework-core, quartz |
| `CO-RABBITMQTRANSPORT-TESTS` | C1 | `src/Transports/ViciOne.ServiceBus.RabbitMqTransport` | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | `UnitArchitecture` | 125 | rabbitmq |
| `CO-ANALYZERS-TESTS` | C1 | `src/ViciOne.ServiceBus.Analyzers`, `src/ViciOne.ServiceBus.Analyzers.CodeFixes`, `src/ViciOne.ServiceBus.Analyzers.Package` | `tests/ViciOne.ServiceBus.Analyzers.Tests` | `tests2/ViciOne.ServiceBus.Analyzers.Tests` | `UnitArchitecture` | 118 | analyzer |
| `CO-ABSTRACTIONS-TESTS` | C1 | `src/ViciOne.ServiceBus.Abstractions` | `tests/ViciOne.ServiceBus.Abstractions.Tests`, `tests/ViciOne.ServiceBus.Tests` | `tests2/ViciOne.ServiceBus.Abstractions.Tests` | `UnitArchitecture` | 117 | abstractions, core |
| `CO-QUARTZINTEGRATION-TESTS` | C1 | `src/Scheduling/ViciOne.ServiceBus.QuartzIntegration` | `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests` | `tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests` | `UnitArchitecture` | 96 | quartz |
| `CO-BENCHMARKS-TESTS` | C1 | `benchmarks/ViciOne.ServiceBus.Benchmark`, `benchmarks/ViciOne.ServiceBus.BenchmarkConsole` | `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` | `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` | `UnitArchitecture` | 75 | benchmarks |
| `CO-MESSAGEPACK-TESTS` | C1 | `src/ViciOne.ServiceBus.MessagePack` | `tests/ViciOne.ServiceBus.Tests` | `tests2/ViciOne.ServiceBus.MessagePack.Tests` | `UnitArchitecture` | 67 | core |
| `CO-DIAGNOSTICS-TESTS` | C1 | `tools/diagnostics/ViciOne.ServiceBus.Diagnostics` | `tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` | `tests2/Tools/ViciOne.ServiceBus.Diagnostics.Tests` | `UnitArchitecture` | 60 | diagnostics |
| `CO-SQLTRANSPORT-TESTS` | C1 | `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql`, `src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer`, `src/ViciOne.ServiceBus/SqlTransport` | `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | `UnitArchitecture` | 59 | sql-transport |
| `CO-ACTIVEMQTRANSPORT-TESTS` | C1 | `src/Transports/ViciOne.ServiceBus.ActiveMqTransport` | `tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` | `UnitArchitecture` | 54 | activemq |
| `CO-ENTITYFRAMEWORKCOREINTEGRATION-TESTS` | C1 | `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration` | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` | `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` | `UnitArchitecture` | 52 | entity-framework-core |
| `CO-SIGNALR-TESTS` | C1 | `src/ViciOne.ServiceBus.SignalR` | `tests/ViciOne.ServiceBus.SignalR.Tests` | `tests2/ViciOne.ServiceBus.SignalR.Tests` | `UnitArchitecture` | 34 | signalr |
| `CO-AMAZONSQSTRANSPORT-TESTS` | C1 | `src/Transports/ViciOne.ServiceBus.AmazonSqsTransport` | `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` | `UnitArchitecture` | 32 | none |
| `CO-AZURE-SERVICEBUS-CORE-TESTS` | C1 | `src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core` | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` | `tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` | `UnitArchitecture` | 31 | none |
| `CO-STATEMACHINEVISUALIZER-TESTS` | C1 | `src/ViciOne.ServiceBus.StateMachineVisualizer` | `tests/ViciOne.ServiceBus.Tests` | `tests2/ViciOne.ServiceBus.StateMachineVisualizer.Tests` | `UnitArchitecture` | 9 | core |
| `CO-AZURE-TABLE-TESTS` | C1 | `src/Persistence/ViciOne.ServiceBus.Azure.Table` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` | `UnitArchitecture` | 3 | none |
| `CO-AMAZONS3-TESTS` | C1 | `src/Persistence/ViciOne.ServiceBus.AmazonS3` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.AmazonS3.Tests` | `UnitArchitecture` | 2 | none |
| `CO-AZURE-STORAGE-TESTS` | C1 | `src/Persistence/ViciOne.ServiceBus.Azure.Storage` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.Tests` | `UnitArchitecture` | 2 | none |
| `CO-DYNAMODBINTEGRATION-TESTS` | C1 | `src/Persistence/ViciOne.ServiceBus.DynamoDbIntegration` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests` | `UnitArchitecture` | 1 | none |
| `CO-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS` | C2 | `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration` | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` | `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests` | `LocalIntegration` | 127 | entity-framework-core |
| `CO-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS` | C2 | `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql` | `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql.IntegrationTests` | `LocalIntegration` | 82 | sql-transport |
| `CO-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS` | C2 | `src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer` | `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer.IntegrationTests` | `LocalIntegration` | 47 | sql-transport |
| `CO-QUARTZINTEGRATION-INTEGRATIONTESTS` | C2 | `src/Scheduling/ViciOne.ServiceBus.QuartzIntegration` | — (new work from gap analysis) | `tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.IntegrationTests` | `LocalIntegration` | 1 | none |
| `CO-RABBITMQTRANSPORT-INTEGRATIONTESTS` | C3 | `src/Transports/ViciOne.ServiceBus.RabbitMqTransport` | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.IntegrationTests` | `LocalIntegration` | 246 | quartz, rabbitmq |
| `CO-ACTIVEMQTRANSPORT-INTEGRATIONTESTS` | C3 | `src/Transports/ViciOne.ServiceBus.ActiveMqTransport` | `tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.IntegrationTests` | `LocalIntegration` | 125 | activemq, quartz |
| `CO-AMAZONSQSTRANSPORT-INTEGRATIONTESTS` | C3 | `src/Transports/ViciOne.ServiceBus.AmazonSqsTransport` | `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.IntegrationTests` | `LocalIntegration` | 60 | none |
| `CO-AZURE-TABLE-INTEGRATIONTESTS` | C3 | `src/Persistence/ViciOne.ServiceBus.Azure.Table` | `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` | `tests2/Persistence/ViciOne.ServiceBus.Azure.Table.IntegrationTests` | `LocalIntegration` | 33 | none |
| `CO-EVENTHUBINTEGRATION-INTEGRATIONTESTS` | C3 | `src/Transports/ViciOne.ServiceBus.EventHubIntegration` | `tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests` | `tests2/Transports/ViciOne.ServiceBus.EventHubIntegration.IntegrationTests` | `LocalIntegration` | 22 | none |
| `CO-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS` | C3 | `src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core` | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` | `tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.IntegrationTests` | `LocalIntegration` | 15 | none |
| `CO-DYNAMODBINTEGRATION-INTEGRATIONTESTS` | C3 | `src/Persistence/ViciOne.ServiceBus.DynamoDbIntegration` | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests` | `tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.IntegrationTests` | `LocalIntegration` | 7 | none |
| `CO-AZURE-STORAGE-INTEGRATIONTESTS` | C3 | `src/Persistence/ViciOne.ServiceBus.Azure.Storage` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.IntegrationTests` | `LocalIntegration` | 5 | none |
| `CO-AMAZONS3-INTEGRATIONTESTS` | C3 | `src/Persistence/ViciOne.ServiceBus.AmazonS3` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.AmazonS3.IntegrationTests` | `LocalIntegration` | 3 | none |
| `CO-AZURE-SERVICEBUS-CORE-EXTERNALTESTS` | C4a | `src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core` | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` | `tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests` | `External` | 108 | none |
| `CO-AMAZONSQSTRANSPORT-EXTERNALTESTS` | C4a | `src/Transports/ViciOne.ServiceBus.AmazonSqsTransport` | `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.ExternalTests` | `External` | 9 | none |
| `CO-AZURE-TABLE-EXTERNALTESTS` | C4a | `src/Persistence/ViciOne.ServiceBus.Azure.Table` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.Azure.Table.ExternalTests` | `External` | 3 | none |
| `CO-EVENTHUBINTEGRATION-EXTERNALTESTS` | C4a | `src/Transports/ViciOne.ServiceBus.EventHubIntegration` | — (new work from gap analysis) | `tests2/Transports/ViciOne.ServiceBus.EventHubIntegration.ExternalTests` | `External` | 3 | none |
| `CO-DYNAMODBINTEGRATION-EXTERNALTESTS` | C4a | `src/Persistence/ViciOne.ServiceBus.DynamoDbIntegration` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.ExternalTests` | `External` | 2 | none |
| `CO-AMAZONS3-EXTERNALTESTS` | C4a | `src/Persistence/ViciOne.ServiceBus.AmazonS3` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.AmazonS3.ExternalTests` | `External` | 1 | none |
| `CO-AZURE-STORAGE-EXTERNALTESTS` | C4a | `src/Persistence/ViciOne.ServiceBus.Azure.Storage` | — (new work from gap analysis) | `tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.ExternalTests` | `External` | 1 | none |
| `CO-RABBITMQTRANSPORT-EXTERNALTESTS` | C4a | `src/Transports/ViciOne.ServiceBus.RabbitMqTransport` | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.ExternalTests` | `External` | 1 | none |


## 3. Cohort ownership in detail

Agent session identifiers are assigned when a cohort starts. The roles and the collision-free write
areas below are fixed now, and no test edit is permitted merely because a wave name exists.

### `CO-ARCHITECTURE-TESTS`

- **Writer** `W-ARCHITECTURE-TESTS` — sole write access to `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ARCHITECTURE-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ARCHITECTURE-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `directory-build-organization`, `msbuild-antipatterns`, `binlog-generation`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj --minimum-expected-tests 20 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/F1/CO-ARCHITECTURE-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 20 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-ARCHITECTURE-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-CORE`

- **Writer** `W-CORE` — sole write access to `tests2/ViciOne.ServiceBus.Tests/**`; no other cohort writes there. The four
  census areas (saga and state machine; container, middleware, pipeline, initializers, configuration;
  serialization, courier, message data, harness surface; observers, request/response, scheduling, job
  service, batching) are **read-only analysis lanes** for specialist agents. Only `W-CORE` edits.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`,
  `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`,
  `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.gitattributes`,
  `global.json`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-CORE` (semantic loss and false green: every obligation against its ledger row
  and its anchor identity) and `RV-STR-CORE` (structure, dependency direction, determinism, assertion
  quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-agent` as the mandatory entry, then
  `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules
  recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --minimum-expected-tests 1805 --report-trx`
  — **one unfiltered project gate over the complete 1805-obligation assembly projection**. Traits may
  serve local diagnosis only; they never own completeness or release evidence, because a per-area
  minimum count can be satisfied by tests from another area.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-CORE/` with command,
  exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: all 1805 obligations terminal; every named new test green in
  `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-CORE` PASS; integrator
  confirms no source, solution, package or profile drift.

### `CO-RABBITMQTRANSPORT-TESTS`

- **Writer** `W-RABBITMQTRANSPORT-TESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-RABBITMQTRANSPORT-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-RABBITMQTRANSPORT-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --minimum-expected-tests 125 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-RABBITMQTRANSPORT-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 125 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-RABBITMQTRANSPORT-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-ANALYZERS-TESTS`

- **Writer** `W-ANALYZERS-TESTS` — sole write access to `tests2/ViciOne.ServiceBus.Analyzers.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ANALYZERS-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ANALYZERS-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/ViciOne.ServiceBus.Analyzers.Tests/ViciOne.ServiceBus.Analyzers.Tests.csproj --minimum-expected-tests 118 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-ANALYZERS-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 118 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-ANALYZERS-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-ABSTRACTIONS-TESTS`

- **Writer** `W-ABSTRACTIONS-TESTS` — sole write access to `tests2/ViciOne.ServiceBus.Abstractions.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ABSTRACTIONS-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ABSTRACTIONS-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/ViciOne.ServiceBus.Abstractions.Tests/ViciOne.ServiceBus.Abstractions.Tests.csproj --minimum-expected-tests 117 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-ABSTRACTIONS-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 117 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-ABSTRACTIONS-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-QUARTZINTEGRATION-TESTS`

- **Writer** `W-QUARTZINTEGRATION-TESTS` — sole write access to `tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-QUARTZINTEGRATION-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-QUARTZINTEGRATION-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests/ViciOne.ServiceBus.QuartzIntegration.Tests.csproj --minimum-expected-tests 96 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-QUARTZINTEGRATION-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 96 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-QUARTZINTEGRATION-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-BENCHMARKS-TESTS`

- **Writer** `W-BENCHMARKS-TESTS` — sole write access to `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-BENCHMARKS-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-BENCHMARKS-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project benchmarks/ViciOne.ServiceBus.Benchmarks.Tests/ViciOne.ServiceBus.Benchmarks.Tests.csproj --minimum-expected-tests 75 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-BENCHMARKS-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 75 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-BENCHMARKS-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-MESSAGEPACK-TESTS`

- **Writer** `W-MESSAGEPACK-TESTS` — sole write access to `tests2/ViciOne.ServiceBus.MessagePack.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-MESSAGEPACK-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-MESSAGEPACK-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/ViciOne.ServiceBus.MessagePack.Tests/ViciOne.ServiceBus.MessagePack.Tests.csproj --minimum-expected-tests 67 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-MESSAGEPACK-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 67 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-MESSAGEPACK-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-DIAGNOSTICS-TESTS`

- **Writer** `W-DIAGNOSTICS-TESTS` — sole write access to `tests2/Tools/ViciOne.ServiceBus.Diagnostics.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-DIAGNOSTICS-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-DIAGNOSTICS-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Tools/ViciOne.ServiceBus.Diagnostics.Tests/ViciOne.ServiceBus.Diagnostics.Tests.csproj --minimum-expected-tests 60 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-DIAGNOSTICS-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 60 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-DIAGNOSTICS-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-SQLTRANSPORT-TESTS`

- **Writer** `W-SQLTRANSPORT-TESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.SqlTransport.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-SQLTRANSPORT-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-SQLTRANSPORT-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.SqlTransport.Tests/ViciOne.ServiceBus.SqlTransport.Tests.csproj --minimum-expected-tests 59 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-SQLTRANSPORT-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 59 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-SQLTRANSPORT-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-ACTIVEMQTRANSPORT-TESTS`

- **Writer** `W-ACTIVEMQTRANSPORT-TESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ACTIVEMQTRANSPORT-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ACTIVEMQTRANSPORT-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests/ViciOne.ServiceBus.ActiveMqTransport.Tests.csproj --minimum-expected-tests 54 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-ACTIVEMQTRANSPORT-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 54 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-ACTIVEMQTRANSPORT-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-ENTITYFRAMEWORKCOREINTEGRATION-TESTS`

- **Writer** `W-ENTITYFRAMEWORKCOREINTEGRATION-TESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ENTITYFRAMEWORKCOREINTEGRATION-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ENTITYFRAMEWORKCOREINTEGRATION-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.csproj --minimum-expected-tests 52 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-ENTITYFRAMEWORKCOREINTEGRATION-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 52 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-ENTITYFRAMEWORKCOREINTEGRATION-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-SIGNALR-TESTS`

- **Writer** `W-SIGNALR-TESTS` — sole write access to `tests2/ViciOne.ServiceBus.SignalR.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-SIGNALR-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-SIGNALR-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj --minimum-expected-tests 34 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-SIGNALR-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 34 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-SIGNALR-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AMAZONSQSTRANSPORT-TESTS`

- **Writer** `W-AMAZONSQSTRANSPORT-TESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AMAZONSQSTRANSPORT-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AMAZONSQSTRANSPORT-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests/ViciOne.ServiceBus.AmazonSqsTransport.Tests.csproj --minimum-expected-tests 32 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-AMAZONSQSTRANSPORT-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 32 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-AMAZONSQSTRANSPORT-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-SERVICEBUS-CORE-TESTS`

- **Writer** `W-AZURE-SERVICEBUS-CORE-TESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-SERVICEBUS-CORE-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-SERVICEBUS-CORE-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests.csproj --minimum-expected-tests 31 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-AZURE-SERVICEBUS-CORE-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 31 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-AZURE-SERVICEBUS-CORE-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-STATEMACHINEVISUALIZER-TESTS`

- **Writer** `W-STATEMACHINEVISUALIZER-TESTS` — sole write access to `tests2/ViciOne.ServiceBus.StateMachineVisualizer.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-STATEMACHINEVISUALIZER-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-STATEMACHINEVISUALIZER-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/ViciOne.ServiceBus.StateMachineVisualizer.Tests/ViciOne.ServiceBus.StateMachineVisualizer.Tests.csproj --minimum-expected-tests 9 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-STATEMACHINEVISUALIZER-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 9 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-STATEMACHINEVISUALIZER-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-TABLE-TESTS`

- **Writer** `W-AZURE-TABLE-TESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-TABLE-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-TABLE-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.Azure.Table.Tests/ViciOne.ServiceBus.Azure.Table.Tests.csproj --minimum-expected-tests 3 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-AZURE-TABLE-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 3 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-AZURE-TABLE-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AMAZONS3-TESTS`

- **Writer** `W-AMAZONS3-TESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AMAZONS3-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AMAZONS3-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/ViciOne.ServiceBus.AmazonS3.Tests.csproj --minimum-expected-tests 2 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-AMAZONS3-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 2 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-AMAZONS3-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-STORAGE-TESTS`

- **Writer** `W-AZURE-STORAGE-TESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-STORAGE-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-STORAGE-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.Tests/ViciOne.ServiceBus.Azure.Storage.Tests.csproj --minimum-expected-tests 2 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-AZURE-STORAGE-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 2 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-AZURE-STORAGE-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-DYNAMODBINTEGRATION-TESTS`

- **Writer** `W-DYNAMODBINTEGRATION-TESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-DYNAMODBINTEGRATION-TESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-DYNAMODBINTEGRATION-TESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-gap-analysis`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/ViciOne.ServiceBus.DynamoDbIntegration.Tests.csproj --minimum-expected-tests 1 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C1/CO-DYNAMODBINTEGRATION-TESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 1 obligations terminal; all named new tests green in `UnitArchitecture`; assertion and gap review without weakening; `RV-SEM-DYNAMODBINTEGRATION-TESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS`

- **Writer** `W-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-anti-patterns`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests.csproj --minimum-expected-tests 127 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C2/CO-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 127 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-ENTITYFRAMEWORKCOREINTEGRATION-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS`

- **Writer** `W-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-anti-patterns`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql.IntegrationTests/ViciOne.ServiceBus.SqlTransport.PostgreSql.IntegrationTests.csproj --minimum-expected-tests 82 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C2/CO-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 82 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-SQLTRANSPORT-POSTGRESQL-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS`

- **Writer** `W-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-anti-patterns`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer.IntegrationTests/ViciOne.ServiceBus.SqlTransport.SqlServer.IntegrationTests.csproj --minimum-expected-tests 47 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C2/CO-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 47 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-SQLTRANSPORT-SQLSERVER-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-QUARTZINTEGRATION-INTEGRATIONTESTS`

- **Writer** `W-QUARTZINTEGRATION-INTEGRATIONTESTS` — sole write access to `tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-QUARTZINTEGRATION-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-QUARTZINTEGRATION-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `assertion-quality`, `test-anti-patterns`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.IntegrationTests/ViciOne.ServiceBus.QuartzIntegration.IntegrationTests.csproj --minimum-expected-tests 1 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C2/CO-QUARTZINTEGRATION-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 1 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-QUARTZINTEGRATION-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-RABBITMQTRANSPORT-INTEGRATIONTESTS`

- **Writer** `W-RABBITMQTRANSPORT-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-RABBITMQTRANSPORT-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-RABBITMQTRANSPORT-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.IntegrationTests/ViciOne.ServiceBus.RabbitMqTransport.IntegrationTests.csproj --minimum-expected-tests 246 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-RABBITMQTRANSPORT-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 246 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-RABBITMQTRANSPORT-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-ACTIVEMQTRANSPORT-INTEGRATIONTESTS`

- **Writer** `W-ACTIVEMQTRANSPORT-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-ACTIVEMQTRANSPORT-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-ACTIVEMQTRANSPORT-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.IntegrationTests/ViciOne.ServiceBus.ActiveMqTransport.IntegrationTests.csproj --minimum-expected-tests 125 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-ACTIVEMQTRANSPORT-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 125 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-ACTIVEMQTRANSPORT-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AMAZONSQSTRANSPORT-INTEGRATIONTESTS`

- **Writer** `W-AMAZONSQSTRANSPORT-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AMAZONSQSTRANSPORT-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AMAZONSQSTRANSPORT-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.IntegrationTests/ViciOne.ServiceBus.AmazonSqsTransport.IntegrationTests.csproj --minimum-expected-tests 60 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-AMAZONSQSTRANSPORT-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 60 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-AMAZONSQSTRANSPORT-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-TABLE-INTEGRATIONTESTS`

- **Writer** `W-AZURE-TABLE-INTEGRATIONTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.Azure.Table.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-TABLE-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-TABLE-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.Azure.Table.IntegrationTests/ViciOne.ServiceBus.Azure.Table.IntegrationTests.csproj --minimum-expected-tests 33 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-AZURE-TABLE-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 33 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-AZURE-TABLE-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-EVENTHUBINTEGRATION-INTEGRATIONTESTS`

- **Writer** `W-EVENTHUBINTEGRATION-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.EventHubIntegration.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-EVENTHUBINTEGRATION-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-EVENTHUBINTEGRATION-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.EventHubIntegration.IntegrationTests/ViciOne.ServiceBus.EventHubIntegration.IntegrationTests.csproj --minimum-expected-tests 22 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-EVENTHUBINTEGRATION-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 22 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-EVENTHUBINTEGRATION-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS`

- **Writer** `W-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.IntegrationTests/ViciOne.ServiceBus.Azure.ServiceBus.Core.IntegrationTests.csproj --minimum-expected-tests 15 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 15 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-AZURE-SERVICEBUS-CORE-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-DYNAMODBINTEGRATION-INTEGRATIONTESTS`

- **Writer** `W-DYNAMODBINTEGRATION-INTEGRATIONTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-DYNAMODBINTEGRATION-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-DYNAMODBINTEGRATION-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.IntegrationTests/ViciOne.ServiceBus.DynamoDbIntegration.IntegrationTests.csproj --minimum-expected-tests 7 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-DYNAMODBINTEGRATION-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 7 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-DYNAMODBINTEGRATION-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-STORAGE-INTEGRATIONTESTS`

- **Writer** `W-AZURE-STORAGE-INTEGRATIONTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-STORAGE-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-STORAGE-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.IntegrationTests/ViciOne.ServiceBus.Azure.Storage.IntegrationTests.csproj --minimum-expected-tests 5 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-AZURE-STORAGE-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 5 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-AZURE-STORAGE-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AMAZONS3-INTEGRATIONTESTS`

- **Writer** `W-AMAZONS3-INTEGRATIONTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.AmazonS3.IntegrationTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AMAZONS3-INTEGRATIONTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AMAZONS3-INTEGRATIONTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-anti-patterns`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.AmazonS3.IntegrationTests/ViciOne.ServiceBus.AmazonS3.IntegrationTests.csproj --minimum-expected-tests 3 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C3/CO-AMAZONS3-INTEGRATIONTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 3 obligations terminal; all named new tests green in `LocalIntegration`; assertion and gap review without weakening; `RV-SEM-AMAZONS3-INTEGRATIONTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-SERVICEBUS-CORE-EXTERNALTESTS`

- **Writer** `W-AZURE-SERVICEBUS-CORE-EXTERNALTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-SERVICEBUS-CORE-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-SERVICEBUS-CORE-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests/ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests.csproj --minimum-expected-tests 108 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-AZURE-SERVICEBUS-CORE-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 108 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-AZURE-SERVICEBUS-CORE-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AMAZONSQSTRANSPORT-EXTERNALTESTS`

- **Writer** `W-AMAZONSQSTRANSPORT-EXTERNALTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AMAZONSQSTRANSPORT-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AMAZONSQSTRANSPORT-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.AmazonSqsTransport.ExternalTests/ViciOne.ServiceBus.AmazonSqsTransport.ExternalTests.csproj --minimum-expected-tests 9 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-AMAZONSQSTRANSPORT-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 9 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-AMAZONSQSTRANSPORT-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-TABLE-EXTERNALTESTS`

- **Writer** `W-AZURE-TABLE-EXTERNALTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.Azure.Table.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-TABLE-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-TABLE-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.Azure.Table.ExternalTests/ViciOne.ServiceBus.Azure.Table.ExternalTests.csproj --minimum-expected-tests 3 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-AZURE-TABLE-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 3 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-AZURE-TABLE-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-EVENTHUBINTEGRATION-EXTERNALTESTS`

- **Writer** `W-EVENTHUBINTEGRATION-EXTERNALTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.EventHubIntegration.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-EVENTHUBINTEGRATION-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-EVENTHUBINTEGRATION-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.EventHubIntegration.ExternalTests/ViciOne.ServiceBus.EventHubIntegration.ExternalTests.csproj --minimum-expected-tests 3 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-EVENTHUBINTEGRATION-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 3 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-EVENTHUBINTEGRATION-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-DYNAMODBINTEGRATION-EXTERNALTESTS`

- **Writer** `W-DYNAMODBINTEGRATION-EXTERNALTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-DYNAMODBINTEGRATION-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-DYNAMODBINTEGRATION-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.ExternalTests/ViciOne.ServiceBus.DynamoDbIntegration.ExternalTests.csproj --minimum-expected-tests 2 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-DYNAMODBINTEGRATION-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 2 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-DYNAMODBINTEGRATION-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AMAZONS3-EXTERNALTESTS`

- **Writer** `W-AMAZONS3-EXTERNALTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.AmazonS3.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AMAZONS3-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AMAZONS3-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.AmazonS3.ExternalTests/ViciOne.ServiceBus.AmazonS3.ExternalTests.csproj --minimum-expected-tests 1 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-AMAZONS3-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 1 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-AMAZONS3-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-AZURE-STORAGE-EXTERNALTESTS`

- **Writer** `W-AZURE-STORAGE-EXTERNALTESTS` — sole write access to `tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-AZURE-STORAGE-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-AZURE-STORAGE-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Persistence/ViciOne.ServiceBus.Azure.Storage.ExternalTests/ViciOne.ServiceBus.Azure.Storage.ExternalTests.csproj --minimum-expected-tests 1 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-AZURE-STORAGE-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 1 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-AZURE-STORAGE-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.

### `CO-RABBITMQTRANSPORT-EXTERNALTESTS`

- **Writer** `W-RABBITMQTRANSPORT-EXTERNALTESTS` — sole write access to `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.ExternalTests/**`; no other cohort writes there.
- **Integrator** `INT-1` — the single owner of every shared file: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, the five solutions, `tests2/Directory.Build.*`, `testconfig.json`, `testsettings.json`, `.editorconfig`, `.gitignore`, `.testagent/**` and all evidence. Writers never touch these.
- **Reviewers** `RV-SEM-RABBITMQTRANSPORT-EXTERNALTESTS` (semantic loss and false green: every obligation of this cohort against its ledger row and its anchor identity) and `RV-STR-RABBITMQTRANSPORT-EXTERNALTESTS` (structure, dependency direction, determinism, assertion quality). Both read-only, both independent of the writer.
- **Skills due before the first edit**: `code-testing-extensions` (.NET), `test-gap-analysis`, `test-smell-detection`; hash and applied rules recorded in `SKILL_BINDING.md` before the cohort starts.
- **Gate command**: `dotnet test --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.ExternalTests/ViciOne.ServiceBus.RabbitMqTransport.ExternalTests.csproj --minimum-expected-tests 1 --report-trx` for the focused run; `dotnet test --solution ViciOne.ServiceBus.Tests.External.slnx --minimum-expected-tests <profile total> --report-trx` for the profile run.
- **Evidence output**: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/C4a/CO-RABBITMQTRANSPORT-EXTERNALTESTS/` with command, exit code, TRX, binlog and the obligation projection plus its Lead-bound SHA-256.
- **Old-cohort deletion precondition**: every one of the 1 obligations terminal; all named new tests green in `External`; assertion and gap review without weakening; `RV-SEM-RABBITMQTRANSPORT-EXTERNALTESTS` PASS; integrator confirms no source, solution, package or profile drift.
## 3b. Execution rules that bind every cohort

These four are mandatory and are recorded here because a cohort may not start without them.

1. **`dotnet-test:code-testing-agent` is the workflow entry.** It is invoked before every
   test-writing cohort, ahead of any other skill and ahead of the first edit. This work package is
   broad scope, so its full contract applies: `.testagent/research.md`, `plan.md` and `status.md`,
   the requirement checklist quoted verbatim, and a final `Requirement | Evidence` table whose rows
   cite exact test names from a run that exited 0.
2. **Every analysis skill is paired with the applicable .NET extension guidance**, and both the
   skill hash and the rules actually applied are recorded in `SKILL_BINDING.md` before the cohort's
   first edit. A hash without applied rules is a binding, never a claim of compliance.
3. **A new test project is registered in its exact profile solution immediately**, and
   solution-level discovery is proven before the cohort may advance — a project that exists but is
   not discovered contributes zero tests while looking complete, which is the failure the
   minimum-count lock exists to catch.
4. **Folders and namespaces inside a target project mirror the production structure** of its source
   owner. The only exceptions are the explicitly named cross-cutting concerns: the architecture test
   project and the test-infrastructure projects under `tests2/Testing/`.

## 4. Wave order

Product Owner priority: everything locally provable is finished to A+ first; cloud follows.

`F1` foundation → `C1` hermetic (2762 obligations) → `C2` local persistence → `C3` local brokers
(773 together) → `C4a` external written and emulator-proven (128) → `C4b` external executed, waits
for access → `F2` TestFramework removal → `P1` promotion → `G1` freeze → `G2` independent review.

Per cohort: complete owner reading → ledger projection bound by the Lead → new test code → focused
MTP run → assertion and gap review → the two independent read-only reviews → only then the old
cohort is removed under the precondition stated for it.

## 5. Design decisions fixed by measurement

- The sentinel is an assembly fixture failing in cleanup; the obligation marker is read at invocation
  through `BeforeAfterTestAttribute`. The extension project references `xunit.v3.extensibility.core`
  only.
- `RequireCompleteObligationSet` provenance follows the `RestoreLockedModeFromCommandLine` shape; a
  project cannot grant itself the flag.
- The nested `Directory.Build.*` import the parent unconditionally; an `Exists` guard is fail-open.
- The new support projects do not take the product namespace `ViciOne.ServiceBus.Testing`, which 61
  files in five shipped assemblies already populate with 62 type names.
- One broker per owning fixture; the inherited assembly-level serialization is not carried over as
  the isolation mechanism.
- SignalR is hermetic throughout: every inherited fixture builds its harness with
  `InMemoryTestHarness` and nothing starts a host. `ViciOne.ServiceBus.SignalR.IntegrationTests` is
  reserved by name with a measured population of zero and is not created empty.

## 6. Blockers

| # | Blocker | Effect | Owner |
|---|---|---|---|
| B-1 | `global.json` write scope | no release-relevant profile run; Lead adds it with the approval | Lead |
| B-2 | real cloud access | `C4b`, `P1`, `G1` wait by design | Product Owner |

## 7. Requirement to evidence

The final `Requirement | Evidence` table quoting every bound Product Owner requirement verbatim is
maintained in `.testagent/status.md` and completed before the freeze.
