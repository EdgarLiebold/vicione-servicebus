---
type: guide
title: "Your first complete application"
description: "Provide a complete host, contract, consumer, request and response, then explain startup and observable outcomes."
tags: [servicebus, learn]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:44:57.086Z
sources:
  - id: openwiki-source-2844497aebb19d23ff323be9
    resource: repo://samples/DeveloperJourneys/Journey04RequestResponse.cs
  - id: openwiki-source-9b5701a4f3c4f34c0f1dcc70
    resource: repo://samples/SuiteComposition/Program.cs
  - id: openwiki-source-85faeb909d35c406a3cc062f
    resource: repo://samples/SuiteComposition/SuiteCompositionTypes.cs
  - id: openwiki-source-a784efd13db311ce669d4825
    resource: repo://src/ViciOne.ServiceBus.Abstractions/ConsumeContext.cs
  - id: openwiki-source-657105112d4e5047539221f9
    resource: repo://src/ViciOne.ServiceBus/Configuration/DefaultEndpointNameFormatter.cs
generated: { by: "codex", at: "2026-10-07T17:44:57.086Z" }
---

# Your first complete application

Read the [whole-system model](../architecture/conceptual-model.md) first. This example makes one basic interaction observable: a host starts a local bus, sends a typed request, activates a consumer and receives a typed response. It deliberately leaves persistence and brokers out so that each added capability can later be understood separately.

## Prerequisites and project

Use a stable .NET 10 SDK and a checkout of this repository. For a local source example, create a console project at `samples/FirstBus` and use this project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="../../src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj" />
  </ItemGroup>
</Project>
```

The framework reference supplies Generic Host/DI infrastructure for this standalone example. Core supplies InMemory and references Abstractions transitively. No RabbitMQ package or broker is needed here.

For a separately packaged application, replace the source reference with the exact Core NuGet version from your approved package feed and pin its dependency graph. This repository's package-only journeys show that delivery model; do not invent a public feed/version from the fork's original version.

## Complete Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using WikiExample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ViciOneServiceBusHostOptions>(options =>
    options.WaitUntilStarted = true);

builder.Services.AddViciOneServiceBus(bus =>
{
    bus.Limits(MessageLimits.Conservative);
    bus.AddConsumer<GetGreetingConsumer>();
    bus.AddRequestClient<GetGreeting>(new Uri(
        $"queue:{DefaultEndpointNameFormatter.Instance.Consumer<GetGreetingConsumer>()}"));
    bus.UsingInMemory((context, transport) =>
        transport.ConfigureEndpoints(context));
});

using IHost host = builder.Build();
await host.StartAsync();

try
{
    await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
    var client = scope.ServiceProvider
        .GetRequiredService<IRequestClient<GetGreeting>>();

    using var deadline =
        new CancellationTokenSource(TimeSpan.FromSeconds(10));

    Response<Greeting> response = await client.GetResponseAsync<Greeting>(
        new GetGreeting("Edgar"), deadline.Token);

    Console.WriteLine(response.Message.Text);
}
finally
{
    await host.StopAsync();
}

namespace WikiExample
{
    public sealed record GetGreeting(string Name);
    public sealed record Greeting(string Text);

    public sealed class GetGreetingConsumer : IConsumer<GetGreeting>
    {
        public Task ConsumeAsync(ConsumeContext<GetGreeting> context) =>
            context.RespondAsync(
                new Greeting($"Hello, {context.Message.Name}!"));
    }
}
```

Run from the repository root:

```bash
dotnet restore samples/FirstBus/FirstBus.csproj
dotnet run --project samples/FirstBus/FirstBus.csproj --no-restore
```

Normal host/bus logs can surround the expected application line:

```text
Hello, Edgar!
```

This line proves the client received the consumer's response in this process. It does not prove database durability or broker acceptance.

## Why each piece exists

The two records define request and response data, independent of the consumer implementation. The consumer implements the asynchronous callback and responds through the received context.

Keep the contracts in a named application namespace. Placing these records in the global namespace caused consumer contract discovery to reject the introductory application during validation.

`AddConsumer` registers activation. `ConfigureEndpoints` creates the actual receive pipeline. The request client's queue address uses the same formatter as automatic endpoint configuration, avoiding a hard-coded naming mismatch.

Limits are mandatory per bus. Waiting for startup makes initial readiness failure observable before issuing the request. The request client is resolved inside a scope so messaging/context services follow DI lifetime rules.

The ten-second cancellation source bounds the example's work from the caller. It is not a scheduled business timeout. The client has its own request machinery and response correlation; [Requests](../messaging/request-and-response.md) explains deadlines versus transport TTL.

The consumer returns the response task, so the runtime observes that work. The finally block stops the host even when requesting fails.

## Change one boundary at a time

To use RabbitMQ, add its package/project, import `ViciOne.ServiceBus.RabbitMq` and replace `UsingInMemory` with the host/credential configuration in [Bus and host](../configuration/bus-and-host.md). Keep matching endpoints and supply an actual broker. This changes carrier behavior, not the greeting contracts.

To publish an event, register an event consumer and use `IPublishEndpoint.PublishAsync`; the [send/publish chapter](../messaging/send-and-publish.md) explains subscriber topology. Publication is not a substitute for waiting for a response.

To persist effects/intents, add the explicit reliable configuration, EF model and transaction ownership described in [Outbox](../reliability/transactions-and-outbox.md). A DbContext registration alone adds no atomic guarantee.

## A fuller existing executable

The repository's [SuiteComposition](../../samples/SuiteComposition/Program.cs) adds SQLite reliable storage, request/response and one-time scheduling. Its DbContext maps reliable records and its code creates the demonstration schema before hosted services start.

Run that project through your normal restored build graph. The sample writes a temporary database and removes its files afterward. Its success line describes a local InMemory/SQLite composition, even though it also loads the RabbitMQ assembly to check package closure.

## Troubleshooting this example

| Failure | Check |
|---|---|
| Missing namespaces/types | Correct current Core reference and framework/DI imports |
| Composition error | One transport and explicit limits |
| Request waits until cancellation | Consumer endpoint configured and address matches formatter |
| Consumer throws | Request fault and original consumer exception |
| Greeting line absent despite send logs | Response path, not only producer acceptance |
| Stop does not complete | Awaited consumer/provider work and cancellation |

See [Deployment and troubleshooting](../operations/deployment-and-troubleshooting.md) for the production versions of these questions.

## Validation boundary

The complete program was compiled with zero warnings/errors and exercised against the existing local Core/Abstractions assemblies. It printed `Hello, Edgar!` and stopped cleanly. The existing SuiteComposition executable also passed its local limits, SQLite reliability, consumer, request and schedule checks. These smoke runs did not rebuild the currently edited product source or execute the full test suite.

The restricted validation environment required `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` to avoid blocking on Generic Host configuration-file watching. This is an environment setting for that smoke run; it does not change the messaging registration.
