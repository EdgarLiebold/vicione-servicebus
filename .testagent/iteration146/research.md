# Iteration 146 — MessagePack research

## Bound source and tests

The packet personally reads all 14 current C# files and 1,344 physical lines in
`ViciOne.ServiceBus.MessagePack`, its project file and all comments. It also reads all 38 owning
C# test/support files and 3,983 lines, the test project and the complete 103-line requirement
projection.

The owner retains a deliberately minimal two-type public surface, one centralized hardened
MessagePack runtime, explicit interface/concrete-map formatters, binary-owned bodies and envelopes,
payload-admission integration, forwarding overlays, exact metadata projection and a single shared
serializer factory. Static pairing maps every product file to at least one owner test. Fresh
instrumentation reports 99.5816% line and 98.2955% branch coverage; no method is below 80% line
coverage and the highest CRAP score is 22.

## Finding and remediation

`ServiceBusMessagePackFormatterResolver` used a process-lifetime
`ConcurrentDictionary<Type, Lazy<IMessagePackFormatter>>`. Every closed interface contract and
closed `MessageData<T>` mapping therefore remained strongly rooted even when its defining dynamic
assembly was collectible. A new `RunAndCollect` regression test proved the unchanged product red.
The resolver now uses `ConditionalWeakTable<Type, Lazy<IMessagePackFormatter>>`, preserving lazy
execution/publication and one shared formatter for every live key without extending key lifetime.

No second source, comment, naming, dependency, placement or public-API finding was identified.
