# ViciOne.ServiceBus

ViciOne.ServiceBus is a modular .NET 10 messaging platform for typed send, publish,
request/response, scheduling, durable delivery, duplicate-safe consumption, sagas, activities,
jobs, mediation, transport providers, persistence providers, diagnostics, and test harnesses.

Applications install only the capability packages they use. The core application API remains
independent of provider SDKs, while provider and advanced extension contracts live in explicit
namespaces and packages.

## Origin and license

This repository was created from a complete, pinned fork of **MassTransit 8.5.10**, upstream commit
`62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, from the
[MassTransit](https://github.com/MassTransit/MassTransit) project, and carries the deliberately
retained and modernised ViciOne capability scope today. The retained and modified code is still
licensed under the **Apache License 2.0**; see [LICENSE.txt](LICENSE.txt) and [COPYRIGHT](COPYRIGHT).

MassTransit copyright 2007–2024 Chris Patterson. ViciOne modifications copyright 2026 Edgar Liebold.
The retained `ZBase32Formatter` uses the
[z-base-32 alphabet](https://philzimmermann.com/docs/human-oriented-base-32-encoding.txt),
and `TextTable` retains inspiration attribution to
[ConsoleTables](https://github.com/khalidabuhakmeh/ConsoleTables).

The [changelog](CHANGELOG.md) indexes source changes, removed capabilities and corrected defects.
For a current Git comparison of the entire repository, including tests, run
`python3 license/repository_diff.py`. It writes a project summary and a grouped
file-level report under `artifacts/policy`. Git retains the exact file history.
Report security findings through the private channel described in [SECURITY.md](SECURITY.md).

## Install and configure

Add the core package and one transport package. The example below uses RabbitMQ:

```csharp
services.AddViciOneServiceBus(bus =>
{
    bus.Limits(MessageLimits.Conservative);
    bus.AddConsumer<SubmitOrderConsumer>();
    bus.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Host("localhost", credentials =>
        {
            credentials.Username("guest");
            credentials.Password("guest");
        });
        rabbit.ConfigureEndpoints(context);
    });
});
```

Every bus must declare message limits and exactly one transport. Optional capabilities such as
reliable messaging, a message journal, sagas, jobs, or Quartz scheduling are enabled explicitly.
The core composition checks fail during host startup for missing limits or transport selection.
Validation of every provider and optional capability is still an [open engineering task](TODO.md).

The eighteen journeys in [samples/DeveloperJourneys](samples/DeveloperJourneys) are compile-tested
against freshly packed NuGet packages. [samples/SuiteComposition](samples/SuiteComposition) is an
executable composition using the core, RabbitMQ, and Entity Framework Core packages.

## Build and test

Use a current stable .NET 10 SDK. Package resolution is locked by the tracked lock files and
`NuGet.config`.

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore -warnaserror
dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore

dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore -warnaserror
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 4900 \
  --max-parallel-test-modules 1
```

See [docs/build.md](docs/build.md) for all build targets, formatting checks, package verification,
and provider-backed test commands.
The [current quality status](docs/quality-status.md) records the latest product-wide measurement
and open reliability finding.

## Reliability and operations

Reliable messaging uses one application-owned store for outbox, inbox, and scheduled records; one
delivery service; one quarantine model; and one typed operations API. Delivery acknowledgements
distinguish storage commit, carrier acceptance, and consumer application. Unsupported durable
transport combinations fail during startup.

- [Reliability model](docs/reliability.md)
- [Provider capabilities](docs/provider-capabilities.json)
- [Amazon S3 message-data deployment](docs/amazon-s3-message-data.md)
- [Observability](docs/observability.md)
- [API layers and packages](docs/api-surface.md)
- [Database deployment](docs/migrations/README.md)

## Deployment

Deploy the application together with the exact package graph restored from its lock files. Apply
the selected reliable-messaging database schema before starting writers, provide the chosen broker
and credentials through the host configuration, and let startup validation reject incomplete
core composition. Export OpenTelemetry signals and expose the registered health checks from the host.

RabbitMQ provides a broker-backed durable-send acceptance boundary; the in-memory transport has a
separate process-local acceptance contract for deterministic tests. Other transport combinations
fail during startup when durable send is selected, as listed in
[provider capabilities](docs/provider-capabilities.json).
