# ViciOne.ServiceBus

ViciOne.ServiceBus is the messaging and saga building block of the ViciOne Suite. It provides
projects, assemblies, packages, namespaces, public types, configuration, wire identities,
diagnostics and tests under a single technical product identity.

This is a private development state. It is intended for publication only after the functional,
security, provenance and publication gates have passed.

## Origin and license

This repository was created from a complete, pinned fork of **MassTransit 8.5.10**, upstream commit
`62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, from the
[MassTransit](https://github.com/MassTransit/MassTransit) project, and carries the deliberately
retained and modernised ViciOne capability scope today. The retained and modified code is licensed
under the **Apache License 2.0**; see [LICENSE.txt](LICENSE.txt), [NOTICE](NOTICE),
[COPYRIGHT](COPYRIGHT) and [MODIFICATIONS.md](MODIFICATIONS.md). The generated
[CHANGELIST.md](CHANGELIST.md) is the section 4(b) record of what changed.

ViciOne changed the technical product identity throughout the repository to `ViciOne.ServiceBus`.
This includes paths, projects, assemblies, package IDs, namespaces and types, configuration keys,
wire headers, MIME types, topology names, telemetry, logs, generators, analyzers, tests, fixtures
and build automation. The rename changes neither the origin nor any Apache-2.0 obligation.

## Build

Requirements:

- .NET SDK 10.0.302 exactly. `global.json` pins it with `rollForward: disable`, so a different SDK
  fails the build instead of silently rolling forward
- access to the package sources configured for the development environment

```bash
dotnet restore ViciOne.ServiceBus.slnx
dotnet build ViciOne.ServiceBus.slnx --configuration Release --no-restore
dotnet test ViciOne.ServiceBus.slnx --configuration Release --no-build --no-restore
dotnet pack ViciOne.ServiceBus.slnx --configuration Release --no-build --no-restore
```

Every runtime, test and benchmark project targets `net10.0`. The Roslyn analyzer is the single
exception and stays on `netstandard2.0`, because the compiler that loads it is not a `net10.0`
process.

The repository contains transport and persistence integration tests that require their external
infrastructure. `tools/ci/run_broker_category.py` starts the pinned fixture for a category and
publishes its endpoint to the run; a fixture that was not started makes the affected tests fail with
a named missing contract rather than falling back to a default host or secret. Missing infrastructure
is reported as an explicit incomplete proof; tests are never silently skipped or weakened.

Build output goes to `artifacts/sdk`, packages to `artifacts/packages`. See
[CONTRIBUTING.md](CONTRIBUTING.md) for the full restore, build, test, pack and review path.

## Scope

The fork keeps in-memory messaging, the RabbitMQ, ActiveMQ, Azure Service Bus, Amazon SQS and SQL
transports, the Azure Event Hubs rider, saga persistence on EF Core, Azure Table and DynamoDB,
message body storage on Amazon S3 and Azure Blob Storage, Quartz scheduling, the job service,
SignalR, MessagePack serialization, the state machine visualizer, the analyzer, the test framework
and the benchmarks.

Capabilities removed by an explicit product decision are recorded in
[MODIFICATIONS.md](MODIFICATIONS.md) and are not part of this source scope.
