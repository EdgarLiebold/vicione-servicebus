// Public package-only Research draft. No configuration-time startup guarantee is inferred.
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using Xunit;

namespace ViciOneReview.CorePackage05;

public static class BatchTimerAssertions
{
    public static async Task RunAsync(string variant, TimeSpan operationTimeout, CancellationToken caller)
    {
        TimeSpan maximum = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);
        TimeSpan interval = variant switch
        {
            "default-too-large" or "custom-long-size" or "custom-long-expire" => TimeSpan.FromMilliseconds(uint.MaxValue),
            "default-maxvalue" => TimeSpan.MaxValue,
            "system-maximum" => maximum,
            "system-fraction" => maximum + TimeSpan.FromTicks(1),
            "system-ordinary" => TimeSpan.FromMinutes(1),
            _ => throw new ArgumentException("Unknown batch variant", nameof(variant))
        };
        bool invalidDefault = variant.StartsWith("default-", StringComparison.Ordinal);
        bool expire = variant == "custom-long-expire";
        bool customLong = variant.StartsWith("custom-", StringComparison.Ordinal);
        var receipts = new ReceiptProbe();
        var delivery = new BatchProbe();
        var contextProvider = new TaskCompletionSource<TimeProvider>(TaskCreationOptions.RunContinuationsAsynchronously);
        var custom = new LongIntervalProvider();
        TimeProvider? selected = customLong ? custom : null;
        string queue = "owned-batch-timer-" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(delivery);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            IConsumerRegistrationConfigurator<BatchReceiver> registration = bus.AddConsumer<BatchReceiver>(consumer =>
                consumer.Options<BatchOptions>(options => options.SetMessageLimit(2).SetTimeLimit(interval)));
            registration.Endpoint(endpoint => endpoint.Name = queue);
            bus.AddConfigureEndpointsCallback((_, endpoint) => endpoint.UseExecute(context =>
            {
                if (selected is not null) context.SetTimeProvider(selected);
                TimeProvider actual = context.GetTimeProvider();
                Assert.Same(selected ?? TimeProvider.System, actual);
                contextProvider.TrySetResult(actual);
            }));
            bus.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
        });
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IBusControl? ownedBus = null;
        IDisposable? ownedObserver = null;
        bool bodyCompleted = false;
        await OwnedLifetime.RunAsync(async () =>
        {
            // Provider-independent options remain valid. A blind global cap breaks custom-long.
            provider.GetRequiredService<IStartupValidator>().Validate();
            IBusControl bus = provider.GetRequiredService<IBusControl>();
            ownedBus = bus;
            ownedObserver = bus.ConnectConsumeObserver(receipts);
            await bus.StartAsync(caller).WaitAsync(operationTimeout, caller);
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri("queue:" + queue), caller);
            Guid firstId = Guid.NewGuid();
            Guid secondId = Guid.NewGuid();
            await endpoint.SendAsync(new BatchItem(1, firstId), caller).WaitAsync(operationTimeout, caller);
            Assert.Same(selected ?? TimeProvider.System, await contextProvider.Task.WaitAsync(operationTimeout, caller));
            if (invalidDefault)
            {
                Receipt receipt = await receipts.First.Task.WaitAsync(operationTimeout, caller);
                Assert.Equal(firstId, receipt.Identity);
                Assert.NotNull(receipt.Failure);
                ConfigurationException configuration = Assert.Single(Flatten(receipt.Failure!)
                    .OfType<ConfigurationException>().Where(x => x.Results.Any(r => r.Key.EndsWith("Batch.TimeLimit", StringComparison.Ordinal))));
                ValidationResult result = Assert.Single(configuration.Results.Where(r => r.Key.EndsWith("Batch.TimeLimit", StringComparison.Ordinal)));
                Assert.Equal(ValidationResultDisposition.Failure, result.Disposition);
                Assert.Contains("4294967294", result.Message, StringComparison.Ordinal);
                Assert.Contains("Set", result.Message, StringComparison.Ordinal);
                Assert.Contains(queue, result.Message, StringComparison.Ordinal);
                Assert.Equal(0, Volatile.Read(ref delivery.Calls));
                // Original raw timer.Change ArgumentOutOfRangeException supplies no owned result.
                bodyCompleted = true;
                return;
            }

            if (customLong)
            {
                Task firstOutcome = await Task.WhenAny(custom.FirstSchedule.Task, receipts.First.Task).WaitAsync(operationTimeout, caller);
                if (firstOutcome == receipts.First.Task)
                    Assert.Null((await receipts.First.Task).Failure);
                Assert.Equal(interval, await custom.FirstSchedule.Task.WaitAsync(operationTimeout, caller));
            }
            if (expire)
                custom.AdvanceAndFire(interval);
            else
                await endpoint.SendAsync(new BatchItem(2, secondId), caller).WaitAsync(operationTimeout, caller);
            Task terminal = await Task.WhenAny(delivery.Delivered.Task, receipts.First.Task).WaitAsync(operationTimeout, caller);
            if (terminal == receipts.First.Task)
                Assert.Null((await receipts.First.Task).Failure);
            IMessageBatch<BatchItem> resultBatch = await delivery.Delivered.Task.WaitAsync(operationTimeout, caller);
            Assert.Equal(expire ? BatchCompletionMode.Time : BatchCompletionMode.Size, resultBatch.Mode);
            Assert.Equal(expire ? 1 : 2, resultBatch.Count);
            Guid[] identities = resultBatch.Select(x => x.Message.Identity).Order().ToArray();
            Assert.Equal((expire ? new[] { firstId } : new[] { firstId, secondId }).Order().ToArray(), identities);
            Assert.Equal(expire ? new[] { 1 } : new[] { 1, 2 }, resultBatch.Select(x => x.Message.Sequence).Order().ToArray());
            Receipt first = await receipts.First.Task.WaitAsync(operationTimeout, caller);
            Assert.Equal(firstId, first.Identity);
            Assert.Null(first.Failure);
            if (!expire)
            {
                Receipt second = await receipts.Second.Task.WaitAsync(operationTimeout, caller);
                Assert.Equal(secondId, second.Identity);
                Assert.Null(second.Failure);
            }
            Assert.Equal(1, Volatile.Read(ref delivery.Calls));
            Assert.Equal(expire ? 1 : 2, Volatile.Read(ref receipts.Count));
            if (customLong)
            {
                Assert.Equal(1, Volatile.Read(ref custom.Creates));
                Assert.Contains(interval, custom.Schedules);
                Assert.Equal(expire ? 1 : 0, Volatile.Read(ref custom.Fires));
                Assert.Equal(1, Volatile.Read(ref custom.Disposals));
            }
            bodyCompleted = true;
        }, async () =>
        {
            if (ownedBus is not null)
                await ownedBus.StopAsync(CancellationToken.None).WaitAsync(operationTimeout);
            if (bodyCompleted)
            {
                // Drain the owned bus before the final exact terminal-count assertion.
                Assert.Equal(invalidDefault ? 0 : 1, Volatile.Read(ref delivery.Calls));
                Assert.Equal(invalidDefault || expire ? 1 : 2, Volatile.Read(ref receipts.Count));
            }
        },
            () => { ownedObserver?.Dispose(); return Task.CompletedTask; },
            () => provider.DisposeAsync().AsTask().WaitAsync(operationTimeout));
    }

    private static IEnumerable<Exception> Flatten(Exception failure)
    {
        yield return failure;
        if (failure is AggregateException aggregate)
            foreach (Exception child in aggregate.InnerExceptions)
                foreach (Exception entry in Flatten(child)) yield return entry;
        else if (failure.InnerException is { } inner)
            foreach (Exception entry in Flatten(inner)) yield return entry;
    }

    public sealed record BatchItem(int Sequence, Guid Identity);
    public sealed class BatchReceiver(BatchProbe probe) : IConsumer<IMessageBatch<BatchItem>>
    {
        public Task ConsumeAsync(ConsumeContext<IMessageBatch<BatchItem>> context)
        {
            Interlocked.Increment(ref probe.Calls);
            probe.Delivered.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }
    public sealed class BatchProbe
    {
        public int Calls;
        public TaskCompletionSource<IMessageBatch<BatchItem>> Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed record Receipt(Guid Identity, Exception? Failure);
    private sealed class ReceiptProbe : IConsumeObserver
    {
        public int Count;
        public TaskCompletionSource<Receipt> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Receipt> Second { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;
        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class => Record(context, null);
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception failure) where T : class => Record(context, failure);
        private Task Record<T>(ConsumeContext<T> context, Exception? failure) where T : class
        {
            if (context.Message is BatchItem item)
            {
                Interlocked.Increment(ref Count);
                (item.Sequence == 1 ? First : Second).TrySetResult(new(item.Identity, failure));
            }
            return Task.CompletedTask;
        }
    }

    // A valid virtual timer supports >System maximum and actually delivers its registered callback.
    // Advancing exactly the scheduled interval avoids a multiweek wall-clock wait.
    private sealed class LongIntervalProvider : TimeProvider
    {
        private readonly ConcurrentQueue<LongTimer> _timers = new();
        private long _elapsedTicks;
        public int Creates;
        public int Disposals;
        public int Fires;
        public ConcurrentQueue<TimeSpan> Schedules { get; } = new();
        public TaskCompletionSource<TimeSpan> FirstSchedule { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Volatile.Read(ref _elapsedTicks);
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2040, 1, 2, 0, 0, 0, TimeSpan.Zero).AddTicks(GetTimestamp());
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new LongTimer(this, callback, state);
            Interlocked.Increment(ref Creates);
            _timers.Enqueue(timer);
            timer.Change(dueTime, period);
            return timer;
        }
        public void AdvanceAndFire(TimeSpan interval)
        {
            Interlocked.Add(ref _elapsedTicks, interval.Ticks);
            foreach (LongTimer timer in _timers) timer.FireIfDue();
        }
        private sealed class LongTimer(LongIntervalProvider owner, TimerCallback callback, object? state) : ITimer
        {
            private readonly object _gate = new();
            private long? _due;
            private long? _period;
            private bool _disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime < TimeSpan.Zero && dueTime != Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(dueTime));
                if (period < TimeSpan.Zero && period != Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(period));
                lock (_gate)
                {
                    if (_disposed) return false;
                    _due = dueTime == Timeout.InfiniteTimeSpan ? null : checked(owner.GetTimestamp() + dueTime.Ticks);
                    _period = period == Timeout.InfiniteTimeSpan ? null : period.Ticks;
                }
                if (dueTime != Timeout.InfiniteTimeSpan)
                { owner.Schedules.Enqueue(dueTime); owner.FirstSchedule.TrySetResult(dueTime); }
                return true;
            }
            public void FireIfDue()
            {
                lock (_gate)
                {
                    if (_disposed || !_due.HasValue || _due > owner.GetTimestamp()) return;
                    _due = _period.HasValue ? checked(owner.GetTimestamp() + _period.Value) : null;
                }
                Interlocked.Increment(ref owner.Fires);
                callback(state);
            }
            public void Dispose()
            {
                lock (_gate)
                {
                    if (_disposed) return;
                    _disposed = true;
                    _due = null;
                }
                Interlocked.Increment(ref owner.Disposals);
            }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
