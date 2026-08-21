# R0 — Microsoft skill `find-untested-sources`, single mandatory execution

Requirement `REQ-TEST-104`: "The Microsoft find-untested-sources skill is executed exactly once
as a static starting point and is followed by complete behavioral gap analysis; file or project
pairing alone cannot close an obligation."

This is that one execution. It is a **static pairing heuristic, not line or branch coverage**,
and it closes no obligation by itself.

## Execution

| Property | Value |
|---|---|
| Engine | Roslyn (C#), chosen because the repository is .NET-only and its namespace disambiguation beats the polyglot identifier overlap |
| Script | `~/.claude/skills/find-untested-sources/scripts/Find-UntestedSources.cs`, SHA-256 `b6f01c528bcfbeae5f4a88c5953ece8274850c902b90f9a591b4e62ed6161e70` |
| Command | `dotnet run scripts/Find-UntestedSources.cs -- <repo-root>` |
| Repository | `repositories/vicione-servicebus` at commit `ae73c6da748e3bc3257dffa4971ee8680e086207` |
| SDK | 10.0.302 |
| Exit code | 0 |
| Elapsed | 1808 ms |
| Raw output | `pairing.json`, SHA-256 `8ced7f1501e9bbaa360774364b694bbb88319f6d2c7e88311db761b47208dd52`, reproducible by the command above; not versioned (large, machine-regenerable) |

## Result

```
source_files 3888   test_files 1070   paired_files 591   untested_files 3297
```

Statically paired: **15.2 %** of product source files.

| Project | paired | unpaired | total | pair % | decls unpaired |
|---|---:|---:|---:|---:|---:|
| `src/ViciOne.ServiceBus` | 242 | 1732 | 1974 | 12.3 | 2160 |
| `src/ViciOne.ServiceBus.Abstractions` | 245 | 556 | 801 | 30.6 | 698 |
| `src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core` | 10 | 209 | 219 | 4.6 | 232 |
| `src/Transports/ViciOne.ServiceBus.RabbitMqTransport` | 19 | 161 | 180 | 10.6 | 180 |
| `src/Transports/ViciOne.ServiceBus.AmazonSqsTransport` | 9 | 153 | 162 | 5.6 | 171 |
| `src/Transports/ViciOne.ServiceBus.ActiveMqTransport` | 12 | 139 | 151 | 7.9 | 157 |
| `src/Transports/ViciOne.ServiceBus.EventHubIntegration` | 6 | 81 | 87 | 6.9 | 89 |
| `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration` | 15 | 57 | 72 | 20.8 | 61 |
| `benchmarks/ViciOne.ServiceBus.Benchmark` | 11 | 43 | 54 | 20.4 | 48 |
| `src/Persistence/ViciOne.ServiceBus.Azure.Table` | 0 | 30 | 30 | 0.0 | 32 |
| `src/ViciOne.ServiceBus.SignalR` | 2 | 28 | 30 | 6.7 | 30 |
| `src/Scheduling/ViciOne.ServiceBus.QuartzIntegration` | 1 | 19 | 20 | 5.0 | 20 |
| `benchmarks/ViciOne.ServiceBus.BenchmarkConsole` | 1 | 18 | 19 | 5.3 | 32 |
| `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql` | 0 | 17 | 17 | 0.0 | 19 |
| `src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer` | 0 | 14 | 14 | 0.0 | 15 |
| `src/Persistence/ViciOne.ServiceBus.DynamoDbIntegration` | 0 | 12 | 12 | 0.0 | 16 |
| `src/ViciOne.ServiceBus.MessagePack` | 6 | 7 | 13 | 46.2 | 7 |
| `src/ViciOne.ServiceBus.Analyzers` | 3 | 6 | 9 | 33.3 | 8 |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage` | 0 | 5 | 5 | 0.0 | 5 |
| `src/Persistence/ViciOne.ServiceBus.AmazonS3` | 0 | 3 | 3 | 0.0 | 3 |
| `src/ViciOne.ServiceBus.StateMachineVisualizer` | 0 | 3 | 3 | 0.0 | 3 |
| `tools/diagnostics/ViciOne.ServiceBus.Diagnostics` | 3 | 3 | 6 | 50.0 | 3 |
| `src/ViciOne.ServiceBus.Analyzers.CodeFixes` | 2 | 0 | 2 | 100.0 | 0 |
| `tools/ci/api_surface.cs` | 0 | 1 | 1 | 0.0 | 0 |

## What this does and does not establish

**Establishes** a prioritized static worklist. Seven projects pair to nothing at all:
`Azure.Table`, `SqlTransport.PostgreSql`, `SqlTransport.SqlServer`, `DynamoDbIntegration`,
`Azure.Storage`, `AmazonS3`, `StateMachineVisualizer`. Two of those — `AmazonS3` and
`Azure.Storage` — are the projects the Lead plan already names as having no inherited tests.

**Does not establish** anything about the other five. `Azure.Table`, `SqlTransport.*` and
`DynamoDbIntegration` each **do** have an inherited test project, so a 0 % pairing score is a
signal to investigate the pairing failure, not evidence of missing tests: the heuristic misses
reflection- and DI-resolved types, extension methods invoked as instance methods, and types
reached only through `var` or target-typed `new()`. Cohorts `R0-PER` and the SQL cohort resolve
each of these against the real code.

**Does not close a single obligation.** Per `REQ-TEST-104` the complete behavioural gap
analysis — public contracts, observable behaviour paths, branches, invariants, error, recovery,
concurrency and serialization boundaries per retained product project — follows this run and is
what actually produces the gap side of the combined semantic ledger.

One further observation for cohort `R0-PY`: `tools/ci/api_surface.cs` is a **C# file inside the
Python tooling directory**. Section 12.2 item 13 of the Lead plan forbids any executable path
under `tools/**` from discovering, counting, classifying or re-judging C# tests or MTP results,
so this file needs an explicit disposition alongside the 55 Python files.
