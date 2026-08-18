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

## ViciOne.ServiceBus NuGet packages

These are the packages this repository builds, one entry per packable project. Capabilities
that the fork removed by an explicit product decision are recorded in MODIFICATIONS.md and
are deliberately absent here rather than listed as if they still shipped.

### Core

* [ViciOne.ServiceBus](https://nuget.org/packages/ViciOne.ServiceBus/)
* [ViciOne.ServiceBus.Abstractions](https://nuget.org/packages/ViciOne.ServiceBus.Abstractions/)

### Transports

* [ViciOne.ServiceBus.ActiveMQ](https://nuget.org/packages/ViciOne.ServiceBus.ActiveMQ/)
* [ViciOne.ServiceBus.AmazonSQS](https://nuget.org/packages/ViciOne.ServiceBus.AmazonSQS/)
* [ViciOne.ServiceBus.Azure.ServiceBus.Core](https://nuget.org/packages/ViciOne.ServiceBus.Azure.ServiceBus.Core/)
* [ViciOne.ServiceBus.RabbitMQ](https://nuget.org/packages/ViciOne.ServiceBus.RabbitMQ/)
* [ViciOne.ServiceBus.SqlTransport.PostgreSQL](https://nuget.org/packages/ViciOne.ServiceBus.SqlTransport.PostgreSQL/)
* [ViciOne.ServiceBus.SqlTransport.SqlServer](https://nuget.org/packages/ViciOne.ServiceBus.SqlTransport.SqlServer/)

### Riders

* [ViciOne.ServiceBus.EventHub](https://nuget.org/packages/ViciOne.ServiceBus.EventHub/)

### Saga persistence

* [ViciOne.ServiceBus.Azure.Table](https://nuget.org/packages/ViciOne.ServiceBus.Azure.Table/)
* [ViciOne.ServiceBus.DynamoDb](https://nuget.org/packages/ViciOne.ServiceBus.DynamoDb/)
* [ViciOne.ServiceBus.EntityFrameworkCore](https://nuget.org/packages/ViciOne.ServiceBus.EntityFrameworkCore/)

### Message data

* [ViciOne.ServiceBus.AmazonS3](https://nuget.org/packages/ViciOne.ServiceBus.AmazonS3/)
* [ViciOne.ServiceBus.Azure.Storage](https://nuget.org/packages/ViciOne.ServiceBus.Azure.Storage/)

### Scheduling

* [ViciOne.ServiceBus.Quartz](https://nuget.org/packages/ViciOne.ServiceBus.Quartz/)

### Interoperability

* [ViciOne.ServiceBus.MessagePack](https://nuget.org/packages/ViciOne.ServiceBus.MessagePack/)

### Other

* [ViciOne.ServiceBus.Analyzers](https://nuget.org/packages/ViciOne.ServiceBus.Analyzers/)
* [ViciOne.ServiceBus.SignalR](https://nuget.org/packages/ViciOne.ServiceBus.SignalR/)
* [ViciOne.ServiceBus.StateMachineVisualizer](https://nuget.org/packages/ViciOne.ServiceBus.StateMachineVisualizer/)
* [ViciOne.ServiceBus.TestFramework](https://nuget.org/packages/ViciOne.ServiceBus.TestFramework/)
