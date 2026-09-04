using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser]
[GcServer(true)]
[GcForce]
public class ChannelBenchmark :
    IAsyncDisposable
{
    readonly TaskExecutor _tightCapacityExecutor = new(capacity: 1, concurrencyLimit: 1);
    readonly TaskExecutor _taskExecutor = new();

    public async ValueTask DisposeAsync()
    {
        await _tightCapacityExecutor.DisposeAsync();
        await _taskExecutor.DisposeAsync();
    }

    [Benchmark(Baseline = true, Description = "Regular Method")]
    public async Task RegularMethodAsync()
    {
        await SubjectMethodAsync().ConfigureAwait(false);
    }

    [Benchmark(Description = "Tight-capacity executor")]
    public async Task TightCapacityExecutorAsync()
    {
        await _tightCapacityExecutor.ExecuteAsync(SubjectMethodAsync).ConfigureAwait(false);
    }

    [Benchmark(Description = "Default-capacity executor")]
    public async Task DefaultCapacityExecutorAsync()
    {
        await _taskExecutor.ExecuteAsync(SubjectMethodAsync).ConfigureAwait(false);
    }

    static async Task SubjectMethodAsync()
    {
        await SubjectChildMethodAsync<long>().ConfigureAwait(false);
    }

    static async Task SubjectChildMethodAsync<T>()
    {
    }
}


[MemoryDiagnoser]
[GcServer(true)]
[GcForce]
public class ConcurrentChannelBenchmark :
    IAsyncDisposable
{
    readonly TaskExecutor _tightCapacityExecutor = new(capacity: 1, concurrencyLimit: 10);
    readonly TaskExecutor _taskExecutor = new(10);

    public async ValueTask DisposeAsync()
    {
        await _tightCapacityExecutor.DisposeAsync();

        await _taskExecutor.DisposeAsync();
    }

    [Benchmark(Baseline = true, Description = "Regular Method", OperationsPerInvoke = 10)]
    public async Task RegularMethodAsync()
    {
        await Parallel.ForAsync(0, 10, async (n, token) => await SubjectMethodAsync());
    }

    [Benchmark(Description = "Tight-capacity executor", OperationsPerInvoke = 10)]
    public async Task TightCapacityExecutorAsync()
    {
        await Parallel.ForAsync(0, 10,
            async (n, token) => await _tightCapacityExecutor.ExecuteAsync(SubjectMethodAsync, token));
    }

    [Benchmark(Description = "Default-capacity executor", OperationsPerInvoke = 10)]
    public async Task DefaultCapacityExecutorAsync()
    {
        await Parallel.ForAsync(0, 10,
            async (n, token) => await _taskExecutor.ExecuteAsync(SubjectMethodAsync, token));
    }

    static async Task SubjectMethodAsync()
    {
        await SubjectChildMethodAsync<long>().ConfigureAwait(false);
    }

    static async Task SubjectChildMethodAsync<T>()
    {
    }

    static async ValueTask SubjectMethodValueAsync()
    {
        await SubjectChildMethodValueAsync<long>().ConfigureAwait(false);
    }

    static async ValueTask SubjectChildMethodValueAsync<T>()
    {
    }
}


public class MessageTypeChannelReader<T> :
    ChannelReader<ConsumeContext<T>>
    where T : class
{
    readonly ChannelReader<ConsumeContext> _source;

    public MessageTypeChannelReader(ChannelReader<ConsumeContext> source)
    {
        _source = source;
    }

    public override bool TryRead(out ConsumeContext<T> item)
    {
        while (_source.TryRead(out var read))
        {
            if (read.TryGetMessage(out ConsumeContext<T> messageContext))
            {
                item = messageContext;
                return true;
            }
        }

        item = null;
        return false;
    }

    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return _source.WaitToReadAsync(cancellationToken);
    }
}
