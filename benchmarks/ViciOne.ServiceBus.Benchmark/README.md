# ViciOne.ServiceBus transport benchmark

This on-demand developer tool measures retained ViciOne.ServiceBus transports and the Entity Framework bus outbox. It is not a test project, package, target-device dependency, or automatic performance gate.

Environment-dependent runs that have not yet been executed are tracked once in [`../ToDo.md`](../ToDo.md). An unchecked item is never a performance or functional pass.

Build from the repository root:

```sh
dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release
```

There is no target-framework property on that line any more, and no explanation about multi-targeted product dependencies: every retained runtime, test and tool project of this repository targets `net10.0` alone. The only exceptions are the two Roslyn components and the analyzer package project, none of which this solution builds.

Display the command-line options:

```sh
dotnet run --project benchmarks/ViciOne.ServiceBus.Benchmark --configuration Release -- --help
```

The bus-outbox scenario requires an explicit Linux-compatible SQL Server connection string. Put it in `VICIONE_BENCHMARK_SQLSERVER_CONNECTION_STRING`, or select a different environment-variable name with `--outbox-db-connection-env`. The value must specify both `Server` and `Initial Catalog`; LocalDB and built-in credential fallbacks are rejected.

The PostgreSQL transport scenario reads its administrator password from `VICIONE_BENCHMARK_POSTGRES_ADMIN_PASSWORD`, or from the environment variable selected with `--admin-password-env`. The benchmark creates a random run-scoped transport password in memory.

To build the optional container image with BuildKit:

```sh
DOCKER_BUILDKIT=1 docker build -f benchmarks/ViciOne.ServiceBus.Benchmark/Dockerfile -t vicione-servicebus-benchmark .
```
