# Diagnostics

Two scenarios that are started deliberately and gate nothing.

They exist because their measurement is not covered elsewhere, and because neither belongs in a
required category: one runs for minutes, the other pushes a hundred thousand messages through a
broker. A required run has to be fast and has to mean the same thing every time; these are
observations a developer asks for when a specific suspicion is on the table.

```
dotnet run --project benchmarks/ViciOne.ServiceBus.Diagnostics -- bus-lifecycle --cycles 240
dotnet run --project benchmarks/ViciOne.ServiceBus.Diagnostics -- publish-load --messages 100000
```

Both need the pinned RabbitMQ fixture of a run and read its endpoint and account from the
environment, exactly as the tests do. There is no default host, port or account: start the fixture
with `tools/ci/run_broker_category.py --broker rabbitmq` and run these inside that environment.

Each writes one JSON object to standard output, or to the file given with `--output`. Neither ever
fails a build: the exit code is non zero only when the scenario could not run at all.

## bus-lifecycle

Starts and stops a bus `--cycles` times and, every `--sample-every` cycles, records how long one
publish and consume round trip took together with the process resources at that moment: thread pool
threads, process threads, pending work items and managed memory.

It replaces `BusLifecycleAccumulation_Probe`, which was an `[Explicit]` fixture inside the required
RabbitMQ category. What it looks for is accumulation across bus lifecycles - a round trip that grows,
or a resource that never comes back - which the latency benchmark does not measure because that one
never restarts a bus.

## publish-load

Publishes `--messages` messages concurrently to one auto delete endpoint and waits until every one of
them has been consumed, then reports the time to publish, the time to complete, and the rate of each.

It replaces `HammerTime_Specs`, which was an `[Explicit]` fixture of the same category. Its subject
is the shape of the load - a hundred thousand publishes in flight at once against a consumer with a
bounded concurrency - and completion is part of the measurement rather than a timeout: the scenario
does not report a rate it did not observe to the last message.
