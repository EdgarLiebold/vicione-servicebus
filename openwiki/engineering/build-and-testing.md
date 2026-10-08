---
type: guide
title: "Building, testing and extending ServiceBus"
description: "Explain solutions, locked builds, harnesses, package gates and advanced/provider extension seams."
tags: [servicebus, engineering]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-d25e33f9734d04ce842df6be
    resource: repo://global.json
  - id: openwiki-source-9b5701a4f3c4f34c0f1dcc70
    resource: repo://samples/SuiteComposition/Program.cs
  - id: openwiki-source-d19dbe5dd288ede31fe66b3c
    resource: repo://src/ViciOne.ServiceBus.Testing/DependencyInjection/ViciOneServiceBusTestingServiceCollectionExtensions.cs
  - id: openwiki-source-8a4f4ba6d1f0b3469a2a0c8a
    resource: repo://tests/Directory.Build.props
  - id: openwiki-source-848a4072ceeb302c7c9b92fe
    resource: repo://tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Building, testing and extending ServiceBus

Verification has several scopes. A strict build checks compilation; package-only samples check delivered API closure; a harness checks configured messaging behavior; provider-backed tests check native infrastructure. Choose evidence matching the boundary you intend to claim.

## Repository solutions

| Solution | Purpose |
|---|---|
| `ViciOne.ServiceBus.slnx` | Shipping product libraries and packages |
| `ViciOne.ServiceBus.Engineering.slnx` | Product plus tools, samples, benchmarks and test graph |
| `ViciOne.ServiceBus.Tests.Unit.slnx` | Hermetic unit and architecture profile |
| `ViciOne.ServiceBus.Tests.LocalIntegration.slnx` | Several isolated local provider services |
| Provider-specific LocalIntegration solutions | RabbitMQ, Azure Service Bus and SQL Server boundaries |

Use a stable .NET 10 SDK. The repository sets Microsoft.Testing.Platform in `global.json`; native tests use xUnit v3. SDK 10 MTP commands use `--solution` or `--project`, with platform arguments passed directly rather than a VSTest-style separator.

## Locked restore and strict build

Run from the repository root:

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode
dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore -warnaserror
dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore -warnaserror
```

Tracked lock files establish the restored graph. Rewriting them is a dependency change, not ordinary verification. Do not combine solution builds with `--no-incremental`: product projects are both solution members and dependencies.

The engineering restore matters for architecture tests that inspect native test projects outside the unit solution. Restoring those imports does not start provider services.

## Unit profile

After the engineering graph is restored:

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore -warnaserror
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit \
  --minimum-expected-tests 4900 --max-parallel-test-modules 1
```

The lower discovery guard is not an exact count or coverage target. Exit status is the verdict; the repository treats warnings and skipped tests as failures. Use current project/framework filter syntax when selecting a subset rather than importing VSTest filter expressions into xUnit v3 MTP.

These are documented procedures, not a claim that this wiki rebuild executed the full suite.

## Application harnesses

The public DI harness composes a bus. If you do not select a transport, it configures InMemory and endpoints, conservative limits and local test scheduling. If you select a transport, it respects that selection. It rejects an already registered default bus instead of silently replacing production composition.

Configuration excerpt:

```csharp
services.AddViciOneServiceBusTestHarness(bus =>
{
    bus.AddConsumer<SubmitOrderConsumer>();
    bus.SetTestTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
});
```

Start through `provider.StartTestHarnessAsync()`, then inspect consumed/sent/published observations and component-specific harness state. Bound waits and dispose owned lifetimes. An observed publication alone does not establish a database effect; assert the application outcome relevant to the test.

Provider testing packages add provider setup/cleanup choices. Cleanup can be destructive to a shared namespace or virtual host; test configuration must select isolated infrastructure.

## Package-consumer gate

`tools/ci/verify_developer_journeys.sh` packs current source into a temporary feed and builds package-only consumers with isolated restoration. DeveloperJourneys has no product project references, so it checks package usability instead of accidentally compiling against an internal project graph.

SuiteComposition runs an InMemory bus with SQLite reliability, a consumer, request and schedule. It also checks the intended loaded assembly closure. Loading RabbitMQ's assembly in that sample does not establish real RabbitMQ acceptance.

The packed public API baseline detects changes to delivered public surfaces. It can prove an API is shipped, including the alternative EF outbox path; it cannot prove that every exposed feature fulfills its conceptual guarantee.

## Provider evidence

Docker-backed profiles create isolated services, capture native logs and remove owned resources. A missing service produces failed or unexecuted verification, not a successful in-memory fallback.

Use the [build guide](../../docs/build.md) for the exact selected provider command and image prerequisites. Broker acceptance, lock renewal, namespace topology and cloud storage integration require evidence from the corresponding carrier/provider.

## Extending the system

Advanced middleware and provider contracts are tested at their ownership boundaries. Architecture tests protect package direction; analyzers help detect blocking/missing-await and contract problems; StateMachineVisualizer renders declared state-machine structure. Benchmark projects measure bounded scenarios, not blanket production performance.

See [Middleware and extensions](middleware-and-extensions.md). The dated [quality report](../../docs/quality-status.md) and [open work](../../TODO.md) preserve bounded results and unresolved verification; this wiki does not promote those into universal approval.
