<!-- ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07. -->
# ViciOne.ServiceBus

ViciOne.ServiceBus provides a developer-focused, modern platform for creating distributed applications without complexity.

- First class testing support
- Write once, then deploy using RabbitMQ, Azure Service Bus, and Amazon SQS
- Observability via Open Telemetry (OTEL)
- Fully-supported, widely-adopted, a complete end-to-end solution

## Documentation

Get started by [reading through the documentation](https://github.com/EdgarLiebold/vicione-servicebus/).

## Build Status

| Branch  |                                                                                              Status                                                                                              |
|---------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
| master  |  [![master](https://github.com/EdgarLiebold/vicione-servicebus/actions/workflows/build.yml/badge.svg?branch=master&event=push)](https://github.com/EdgarLiebold/vicione-servicebus/actions/workflows/build.yml)  |
| develop | [![develop](https://github.com/EdgarLiebold/vicione-servicebus/actions/workflows/build.yml/badge.svg?branch=develop&event=push)](https://github.com/EdgarLiebold/vicione-servicebus/actions/workflows/build.yml) |

## ViciOne.ServiceBus NuGet Packages

The following NuGet packages are the currently supported.

[![alt ViciOne.ServiceBus](https://img.shields.io/nuget/v/ViciOne.ServiceBus.svg "ViciOne.ServiceBus")](https://nuget.org/packages/ViciOne.ServiceBus/)

* [ViciOne.ServiceBus](https://nuget.org/packages/ViciOne.ServiceBus/)
* [ViciOne.ServiceBus.Abstractions](https://www.nuget.org/packages/ViciOne.ServiceBus.Abstractions/)

### Transports

* [ViciOne.ServiceBus.ActiveMQ](https://nuget.org/packages/ViciOne.ServiceBus.ActiveMQ/)
* [ViciOne.ServiceBus.AmazonSQS](https://nuget.org/packages/ViciOne.ServiceBus.AmazonSQS/)
* [ViciOne.ServiceBus.Azure.ServiceBus.Core](https://nuget.org/packages/ViciOne.ServiceBus.Azure.ServiceBus.Core/)
    * [ViciOne.ServiceBus.WebJobs.ServiceBus](https://nuget.org/packages/ViciOne.ServiceBus.WebJobs.ServiceBus/)
    * [ViciOne.ServiceBus.WebJobs.EventHubs](https://nuget.org/packages/ViciOne.ServiceBus.WebJobs.EventHubs/)
* [ViciOne.ServiceBus.RabbitMQ](https://nuget.org/packages/ViciOne.ServiceBus.RabbitMQ/)
* **Riders**
    * [ViciOne.ServiceBus.EventHub](https://nuget.org/packages/ViciOne.ServiceBus.EventHub/)
    * [ViciOne.ServiceBus.Kafka](https://nuget.org/packages/ViciOne.ServiceBus.Kafka/)

### Saga Persistence

* [ViciOne.ServiceBus.AmazonS3](https://nuget.org/packages/ViciOne.ServiceBus.AmazonS3/)
* [ViciOne.ServiceBus.Azure.Cosmos](https://nuget.org/packages/ViciOne.ServiceBus.Azure.Cosmos/)
* [ViciOne.ServiceBus.Azure.Cosmos.Table](https://nuget.org/packages/ViciOne.ServiceBus.Azure.Cosmos.Table/)
* [ViciOne.ServiceBus.DapperIntegration](https://nuget.org/packages/ViciOne.ServiceBus.DapperIntegration/)
* [ViciOne.ServiceBus.DynamoDb](https://nuget.org/packages/ViciOne.ServiceBus.DynamoDb/)
* [ViciOne.ServiceBus.EntityFramework](https://nuget.org/packages/ViciOne.ServiceBus.EntityFramework/)
* [ViciOne.ServiceBus.EntityFrameworkCore](https://nuget.org/packages/ViciOne.ServiceBus.EntityFrameworkCore/)
* [ViciOne.ServiceBus.Marten](https://nuget.org/packages/ViciOne.ServiceBus.Marten/)
* [ViciOne.ServiceBus.MongoDb](https://nuget.org/packages/ViciOne.ServiceBus.MongoDb/)
* [ViciOne.ServiceBus.NHibernate](https://nuget.org/packages/ViciOne.ServiceBus.NHibernate/)
* [ViciOne.ServiceBus.Redis](https://nuget.org/packages/ViciOne.ServiceBus.Redis/)

### Message Data

* [ViciOne.ServiceBus.Azure.Storage](https://nuget.org/packages/ViciOne.ServiceBus.Azure.Storage/)

### Scheduling

* [ViciOne.ServiceBus.Hangfire](https://nuget.org/packages/ViciOne.ServiceBus.Hangfire/)
* [ViciOne.ServiceBus.Quartz](https://nuget.org/packages/ViciOne.ServiceBus.Quartz/)

### Interoperability

* [ViciOne.ServiceBus.Interop.NServiceBus](https://nuget.org/packages/ViciOne.ServiceBus.Interop.NServiceBus/)
* [ViciOne.ServiceBus.Newtonsoft](https://nuget.org/packages/ViciOne.ServiceBus.Newtonsoft/)

### Other

* [ViciOne.ServiceBus.Analyzers](https://nuget.org/packages/ViciOne.ServiceBus.Analyzers/)
* [ViciOne.ServiceBus.SignalR](https://nuget.org/packages/ViciOne.ServiceBus.SignalR/)
* [ViciOne.ServiceBus.Prometheus](https://nuget.org/packages/ViciOne.ServiceBus.Prometheus/)
* [ViciOne.ServiceBus.StateMachineVisualizer](https://nuget.org/packages/ViciOne.ServiceBus.StateMachineVisualizer/)
* [ViciOne.ServiceBus.TestFramework](https://nuget.org/packages/ViciOne.ServiceBus.TestFramework/)

## Deprecated Packages

The following packages from earlier versions of ViciOne.ServiceBus are no longer supported.

* Automatonymous
* Automatonymous.NHibernate
* Automatonymous.Visualizer
* GreenPipes
* ViciOne.ServiceBus.ApplicationInsights
* ViciOne.ServiceBus.AspNetCore
* ViciOne.ServiceBus.Autofac
* ViciOne.ServiceBus.Automatonymous
* ViciOne.ServiceBus.Automatonymous.Autofac
* ViciOne.ServiceBus.Automatonymous.Extensions.DependencyInjection
* ViciOne.ServiceBus.Automatonymous.Lamar
* ViciOne.ServiceBus.Automatonymous.SimpleInjector
* ViciOne.ServiceBus.Automatonymous.StructureMap
* ViciOne.ServiceBus.Automatonymous.Windsor
* ViciOne.ServiceBus.AzureServiceBus
* ViciOne.ServiceBus.CastleWindsor
* ViciOne.ServiceBus.Extensions.DependencyInjection
* ViciOne.ServiceBus.Extensions.Logging
* ViciOne.ServiceBus.Host
* ViciOne.ServiceBus.Http
* ViciOne.ServiceBus.Lamar
* ViciOne.ServiceBus.Log4Net
* ViciOne.ServiceBus.MSMQ
* ViciOne.ServiceBus.Ninject
* ViciOne.ServiceBus.NLog
* ViciOne.ServiceBus.Platform.Abstractions
* ViciOne.ServiceBus.Reactive
* ViciOne.ServiceBus.SerilogIntegration
* ViciOne.ServiceBus.SimpleInjector
* ViciOne.ServiceBus.StructureMap
* ViciOne.ServiceBus.StructureMapSigned
* ViciOne.ServiceBus.Unity

## Discord

Get help live at the ViciOne.ServiceBus Discord server.

[![alt Join the conversation](https://img.shields.io/discord/682238261753675864.svg "Discord")](https://discord.gg/rNpQgYn)

## GitHub Issues

> Please do not open an issue on GitHub, unless you have spotted an actual bug in ViciOne.ServiceBus.

Use [GitHub Discussions](https://github.com/EdgarLiebold/vicione-servicebus/discussions) to ask questions, bring up ideas, or other general items. Issues are not the
place for questions, and will either be converted to a discussion or closed.
