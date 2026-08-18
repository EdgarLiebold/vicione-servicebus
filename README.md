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

- .NET SDK 10.0.302 exactly, pinned by `global.json` with `rollForward: disable`
- exactly one package source, nuget.org, named in `NuGet.config`; the machine's own configuration
  does not participate

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet build   ViciOne.ServiceBus.slnx -c Release --no-restore
dotnet pack    ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
```

Two solutions sit at the root, so every command names the one it means. Every project resolves
against a tracked `packages.lock.json`; updating a package is the single documented exception.

There is no single command that tests this product locally: a blanket `dotnet test` would start test
projects whose fixtures are not running, and the capabilities that need a real cloud resource have no
local fixture at all. Each category is started through its runner, which owns its fixture.

[docs/build.md](docs/build.md) carries the whole contract - the two solutions, the locked restore and
how a package is updated, the three Roslyn projects that are the `netstandard2.0` exception, the five
central build gates, the verification model and how each category is run.

## Scope

The fork keeps in-memory messaging, the RabbitMQ, ActiveMQ, Azure Service Bus, Amazon SQS and SQL
transports, the Azure Event Hubs rider, saga persistence on EF Core, Azure Table and DynamoDB,
message body storage on Amazon S3 and Azure Blob Storage, Quartz scheduling, the job service,
SignalR, MessagePack serialization, the state machine visualizer, the analyzer, the test framework
and the benchmarks.

Capabilities removed by an explicit product decision are recorded in
[MODIFICATIONS.md](MODIFICATIONS.md) and are not part of this source scope.
