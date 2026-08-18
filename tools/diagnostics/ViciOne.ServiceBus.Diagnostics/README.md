# Diagnostics

Two scenarios that are started deliberately and gate nothing.

They exist because their measurement is not covered elsewhere, and because neither belongs in a
required category: one runs for minutes, the other pushes a hundred thousand messages through a
broker. A required run has to be fast and has to mean the same thing every time; these are
observations a developer asks for when a specific suspicion is on the table.

## How to start them

Through the canonical runner, which owns the fixture. It starts the pinned broker, hands the endpoints
and the run-scoped account to the child alone, and removes the fixture afterwards.

```
python3 tools/ci/run_broker_category.py --broker rabbitmq --command -- \
  dotnet run --project tools/diagnostics/ViciOne.ServiceBus.Diagnostics -c Release -- \
  bus-lifecycle --cycles 240 --sample-every 20

python3 tools/ci/run_broker_category.py --broker rabbitmq --command -- \
  dotnet run --project tools/diagnostics/ViciOne.ServiceBus.Diagnostics -c Release -- \
  publish-load --messages 100000 --output artifacts/run-output/publish-load.json
```

Running `dotnet run` on its own does not work and is not meant to: there is no default host, port or
account to fall back to, so a scenario outside that environment refuses to start rather than measuring
whatever broker happens to be on the machine.

The command line is closed. An unknown option, an option given twice, an option without a value, a
positional argument and a `--sample-every` larger than `--cycles` are each refused with their own
reason. Success and failure are both one JSON object; the exit code is non zero only when the scenario
could not run, never because a number was worse than another number.

## bus-lifecycle

Starts and stops a bus `--cycles` times and, every `--sample-every` cycles, records the start, the
round trip and the stop separately, together with the process resources at that moment: thread pool
threads, process threads, pending work items and managed memory.

The three durations are kept apart on purpose. Their sum is reported as `cycleMilliseconds`; calling
that sum a round trip would name the start and the stop as latency.

It replaces `BusLifecycleAccumulation_Probe`, which was an `[Explicit]` fixture inside the required
RabbitMQ category. What it looks for is accumulation across bus lifecycles - a round trip that grows,
or a resource that never comes back - which the latency benchmark does not measure because that one
never restarts a bus.

## publish-load

Publishes `--messages` messages concurrently to one auto delete endpoint and waits until every one of
them has been consumed, then reports the time to publish, the time to complete, and the rate of each.

Every message carries a sequence this run owns, and completion means every one of those sequences was
seen exactly once. A count of consume events cannot say that: one message delivered twice and another
lost reaches the same number. The result therefore names `uniqueConsumed`, `missing`, `duplicates` and
`outOfRange`, and `completedPerSecond` is null unless the set was complete, because a rate for an
incomplete set invites the wrong conclusion.

It replaces `HammerTime_Specs`, which was an `[Explicit]` fixture of the same category. Its subject is
the shape of the load - a hundred thousand publishes in flight at once against a consumer with a
bounded concurrency - and completion is part of the measurement rather than a timeout.
