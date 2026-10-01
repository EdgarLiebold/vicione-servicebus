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
  publish-load --messages 100000
```

Without `--output` the result goes to stdout, which belongs to the run that asked for it. A fixed path
under `artifacts/run-output/` stood here and contradicted the rule the rest of this repository holds:
one run owns all of its mutable output, and two runs writing the same file means the second one's
result is read as the first one's. A caller who wants a file names one that belongs to the run - the
runner prints its own run root, and `$VICIONE_SERVICEBUS_RUN_ROOT` carries it into the child:

```
--output "$VICIONE_SERVICEBUS_RUN_ROOT/publish-load.json"
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

Publishes `--messages` messages concurrently to one auto delete endpoint, waits until every identity
this run owns has arrived at least once, keeps observing for a further three seconds and then reads
what it holds.

Two questions are kept apart. That every identity arrived is what the wait ends on. That the
observation was *exact* is a different one: every identity seen exactly once, nothing seen twice, and
nothing seen that this run never published. `outcome` is `exact`, `invalid`, `inconclusive` or
`timeout`, and `completedPerSecond` is reported only for `exact` - a rate for a run that lost or
duplicated a message invites the wrong conclusion.

Between the wait and the verdict there are three steps, in this order, and `observationBoundary` names
all three. The three second window after the last first seen identity, which is why a duplicate
delivered a moment later is still counted. Then a bounded stop of the bus. Then the snapshot.

What the stop proves is stated exactly, because the transport does not support a stronger sentence. A
stop that finished *while its budget still held* went through the consumer agent's delivery-complete
wait, and is interpreted as quiescence. A stop whose budget expired proves nothing: the same method
catches that cancellation, cancels the pending consumers and completes anyway. The ledger is
scanned without a lock - a lock would put contention into the path this diagnostic measures - so a
snapshot read while handlers were still running belongs to no single moment of the run, and that is
what `inconclusive` says: the stop did not finish inside its budget, so exactness is not claimed even
when the numbers look exact. Cancellation is raised rather than reported as an incomplete set, because
it is not an answer about the messages.

It replaces `HammerTime_Specs`, which was an `[Explicit]` fixture of the required RabbitMQ category.
Its subject is the shape of the load - a hundred thousand publishes in flight at once against a
consumer with a bounded concurrency - and completion is part of the measurement rather than a timeout.

## Tests

`tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` holds the correctness cases of this tool: the
ledger's exact, missing, duplicate, stranger, late duplicate, timeout and cancellation behaviour, the
observation boundary - standstill before the snapshot, and no exactness without one - the sink a result
is written to on both the success and the failure path, and the command line's refusals.

The test project is part of `ViciOne.ServiceBus.Tests.Unit.slnx`. The required
`unit-architecture` CI job runs that solution without a test filter and enforces
the solution's minimum executed-test count. The measurement scenarios remain on
demand and gate nothing.
