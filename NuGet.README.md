# ViciOne.ServiceBus

ViciOne.ServiceBus provides a developer-focused, modern platform for creating distributed applications without complexity.

- First class testing support
- Write once, then deploy using RabbitMQ, Azure Service Bus, Amazon SQS, ActiveMQ or a SQL database
- Observability via Open Telemetry (OTEL)

The repository README and CONTRIBUTING describe how to build, test and pack this product. There is
no separate documentation site yet, and none is claimed here.

## ViciOne.ServiceBus NuGet packages

These are the packages this repository builds, one entry per packable project. Capabilities
that the fork removed by an explicit product decision are recorded in MODIFICATIONS.md and
are deliberately absent here rather than listed as if they still shipped.

### Core

* [ViciOne.ServiceBus](https://nuget.org/packages/ViciOne.ServiceBus/)
* [ViciOne.ServiceBus.Abstractions](https://nuget.org/packages/ViciOne.ServiceBus.Abstractions/)

### Transports

* [ViciOne.ServiceBus.ActiveMq](https://nuget.org/packages/ViciOne.ServiceBus.ActiveMq/)
* [ViciOne.ServiceBus.AmazonSqs](https://nuget.org/packages/ViciOne.ServiceBus.AmazonSqs/)
* [ViciOne.ServiceBus.AzureServiceBus](https://nuget.org/packages/ViciOne.ServiceBus.AzureServiceBus/)
* [ViciOne.ServiceBus.RabbitMq](https://nuget.org/packages/ViciOne.ServiceBus.RabbitMq/)
* [ViciOne.ServiceBus.SqlTransport.PostgreSql](https://nuget.org/packages/ViciOne.ServiceBus.SqlTransport.PostgreSql/)
* [ViciOne.ServiceBus.SqlTransport.SqlServer](https://nuget.org/packages/ViciOne.ServiceBus.SqlTransport.SqlServer/)

### Riders

* [ViciOne.ServiceBus.EventHubs](https://nuget.org/packages/ViciOne.ServiceBus.EventHubs/)

### Capabilities

* [ViciOne.ServiceBus.Courier](https://nuget.org/packages/ViciOne.ServiceBus.Courier/)
* [ViciOne.ServiceBus.Futures](https://nuget.org/packages/ViciOne.ServiceBus.Futures/)
* [ViciOne.ServiceBus.Initializers](https://nuget.org/packages/ViciOne.ServiceBus.Initializers/)
* [ViciOne.ServiceBus.JobService](https://nuget.org/packages/ViciOne.ServiceBus.JobService/)
* [ViciOne.ServiceBus.Mediator](https://nuget.org/packages/ViciOne.ServiceBus.Mediator/)
* [ViciOne.ServiceBus.Sagas](https://nuget.org/packages/ViciOne.ServiceBus.Sagas/)

### Saga persistence

* [ViciOne.ServiceBus.Azure.Table](https://nuget.org/packages/ViciOne.ServiceBus.Azure.Table/)
* [ViciOne.ServiceBus.DynamoDb](https://nuget.org/packages/ViciOne.ServiceBus.DynamoDb/)
* [ViciOne.ServiceBus.EntityFrameworkCore](https://nuget.org/packages/ViciOne.ServiceBus.EntityFrameworkCore/)
* [ViciOne.ServiceBus.EntityFrameworkCore.Sagas](https://nuget.org/packages/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/)

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

### Testing

* [ViciOne.ServiceBus.Testing](https://nuget.org/packages/ViciOne.ServiceBus.Testing/)
* [ViciOne.ServiceBus.AzureServiceBus.Testing](https://nuget.org/packages/ViciOne.ServiceBus.AzureServiceBus.Testing/)
* [ViciOne.ServiceBus.EventHubs.Testing](https://nuget.org/packages/ViciOne.ServiceBus.EventHubs.Testing/)
* [ViciOne.ServiceBus.RabbitMq.Testing](https://nuget.org/packages/ViciOne.ServiceBus.RabbitMq.Testing/)

The inherited `ViciOne.ServiceBus.TestFramework` source has been retired after its useful behavior
material moved into the native test estate; it is not a public package.
