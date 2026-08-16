# Benchmark execution tasks

The benchmark tools compile and their local InMemory paths have been executed. The following environment-dependent runs are deliberately still open. They are not tests, performance budgets, or claimed product baselines.

## External transport runs

- [ ] Run message latency against RabbitMQ with TLS and certificate validation enabled.
- [ ] Run message latency and request/response against a short-lived real Azure Service Bus namespace.
- [ ] Run message latency against a short-lived Amazon SQS test resource.
- [ ] Run message latency against an isolated ActiveMQ broker.
- [ ] Run message latency against an isolated PostgreSQL SQL-transport database.
- [ ] Run request/response against RabbitMQ with TLS and certificate validation enabled.

## SQL Server bus-outbox runs

- [ ] Run the SQL Server Entity Framework bus-outbox path with the InMemory transport.
- [ ] Run the same SQL Server bus-outbox path with RabbitMQ.
- [ ] Run the same SQL Server bus-outbox path with a short-lived real Azure Service Bus namespace.

## Completion evidence

Each completed item records the exact Git commit, runtime and host details, sanitized effective configuration, message count, process exit code, full output artifact, and resource cleanup result. Cloud and broker credentials never enter the repository. A run is complete only when every requested message is represented in the reported sample count and the process exits successfully.
