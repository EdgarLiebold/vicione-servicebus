# Batch time limits and the effective time provider

`BatchOptions.TimeLimit` must be positive. Batch configuration also validates
message count, concurrency, their capacity product, and `TimeLimitStart` before
the runtime collector is connected.

The timer provider is selected from the first message context when a new batch
is created. A context without an explicit provider uses `TimeProvider.System`.
Registering a `TimeProvider` with dependency injection does not by itself attach
that provider to message contexts.

For the actual `TimeProvider.System` instance, the collection interval truncated
to whole milliseconds must not exceed **4,294,967,294 milliseconds**. The collector
rejects a larger interval with a `ConfigurationException` and a `Batch.TimeLimit`
validation result that identifies the owning bus and receive endpoint and explains
how to correct the configuration. This happens before creating the batch consumer,
creating its timer, or admitting its first message to that batch.

This provider-dependent check runs when creating a batch, after the bus and its
receivers have started. It is not a startup check and does not precede transport
receipt, grouping-key evaluation, or every upstream application callback. Positive
sub-millisecond intervals and the maximum interval plus a fractional millisecond
use the same whole-millisecond conversion as the system timer.

A pipeline may attach its chosen provider before the batching stage. For example,
with `ViciOne.ServiceBus.Advanced` and `ViciOne.ServiceBus.Configuration` in scope
and an application-owned `batchTimeProvider`:

```csharp
endpoint.UseExecute(context => context.SetTimeProvider(batchTimeProvider));
```

The application must place that configuration before the batching stage. An
already open batch keeps the provider selected at its creation; a later context
override changes the provider selection only for a newly created batch.

An application-defined provider owns the range supported by its `CreateTimer`
and `ITimer.Change` implementations. A custom provider may support intervals
longer than the system limit. A custom provider that delegates to system timers
is still responsible for those timers' limits. The system-specific guard is an
identity check against `TimeProvider.System`; it does not discover arbitrary
custom-provider capabilities. A custom timer can reject an interval during
`Change` after batch bookkeeping has begun, and that failure follows the existing
batch fault and cleanup path. No universal custom-provider pre-admission check
or guarantee about `CancellationTokenSource` timeout ranges follows from this API.

The original F-SB-API-030 review requested rejection before receiver startup.
The explicit autonomous contract disposition in
`vicione-architecture/work/CURRENT_ORDER.yaml` replaces that timing requirement
with the provider-specific batch-creation boundary above, preserving the existing
public context-level provider selection. The original finding and its failing
startup evidence remain archived. This disposition is a contract change, not a
claim that the original startup requirement was implemented.
