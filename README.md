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

## Migration and API policy

ViciOne.ServiceBus preserves useful messaging capabilities, not the historical MassTransit API.
Source, binary, overload, naming, namespace, and call-shape compatibility with MassTransit are not
product requirements. Public APIs may change whenever a clearer, safer, more coherent greenfield
design provides the retained capability better. Compatibility shims are not introduced merely to
keep an inherited call site compiling.

Inherited source, tests, documentation, and history remain valuable evidence for capabilities,
failure modes, and design intent. They are inputs to analysis, never an API specification. Each
migration cohort derives its behavior contract from the complete connected product path, preserves
all useful features unless an explicit product decision removes one, corrects product defects at
their source, and protects the resulting A+ API with native behavior tests.

## Build

- .NET SDK 10.0.302 exactly, pinned by `global.json` with `rollForward: disable`
- exactly one package source, nuget.org, named in `NuGet.config`; the machine's own configuration
  does not participate

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet build   ViciOne.ServiceBus.slnx -c Release --no-restore
dotnet pack    ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
```

The repository has separate product, engineering, and materialized native-test profile targets, so
every command names the one it means. Every project resolves against a tracked `packages.lock.json`;
updating a package is the single documented exception.

The native replacement test estate uses xUnit 4 on Microsoft Testing Platform 2. Its currently
materialized hermetic profile runs directly through the .NET 10 CLI:

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 2032 \
  --max-parallel-test-modules 1
```

Local-integration and external profiles appear only with their first executable cohort. The legacy
NUnit/VSTest tests and the remaining temporary takeover runners are behavior evidence during migration;
they are not the final test architecture. The Python policy validator was a discarded Team 1 detour,
not imported product or test behavior. It and its self-test suite are deleted and must not be recreated.
Any independently valid invariant belongs in MSBuild or native xUnit/MTP architecture tests.
The native test executable uses only the Microsoft Testing Platform entry point. Its single central
`tests2/testconfig.json` makes skips and warnings fail and is copied into each artifact under MTP's
assembly-specific configuration name.

[docs/build.md](docs/build.md) carries the complete current build, native-test, profile, configuration,
and migration contract. Bounded work deliberately deferred from the active slice is visible in
[TODO.md](TODO.md); it is not a second architecture or feature catalog.

## Observability

ViciOne.ServiceBus emits one bounded OpenTelemetry schema through the built-in .NET `Meter` and
`ActivitySource` APIs. Dependency-injection registrations activate metrics automatically without
replacing an application-owned `IMeterFactory`; non-DI configurations opt in with
`UseInstrumentation()`. The application remains the sole owner of exporters and sampling.

[docs/observability.md](docs/observability.md) defines the stable source names, instruments,
attributes, activation paths and isolation guarantees. StatsD, Windows performance counters and
freely extensible metric-tag dictionaries are deliberately not parallel telemetry surfaces.

## Scope

The fork keeps in-memory messaging, the RabbitMQ, ActiveMQ, Azure Service Bus, Amazon SQS and SQL
transports, the Azure Event Hubs rider, saga persistence on EF Core, Azure Table and DynamoDB,
message body storage on Amazon S3 and Azure Blob Storage, Quartz scheduling, the job service,
SignalR, MessagePack serialization, the state machine visualizer, the analyzer and the benchmarks.

It also provides an optional, default-off `MessageJournal` for policy-selected and sanitized
diagnostic snapshots of terminal send, publish and consume outcomes. EF Core and Azure Table stores
apply finite size, count and age limits on every append. This capability is not a queue, retry path,
queryable audit history, or owner of ViciOne Suite operational and security audit data.

The inherited TestFramework source remains only as migration input until every behavior obligation
has moved into the native test estate. It is not part of the future public package surface.

Capabilities removed by an explicit product decision are recorded in
[MODIFICATIONS.md](MODIFICATIONS.md) and are not part of this source scope.
