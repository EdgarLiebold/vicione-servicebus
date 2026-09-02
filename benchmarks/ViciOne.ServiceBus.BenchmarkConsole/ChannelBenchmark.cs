namespace ViciOne.ServiceBus.BenchmarkConsole;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Util;


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
    public async Task RegularMethod()
    {
        await SubjectMethod().ConfigureAwait(false);
    }

    [Benchmark(Description = "Tight-capacity executor")]
    public async Task TightCapacityExecutor()
    {
        await _tightCapacityExecutor.ExecuteAsync(SubjectMethod).ConfigureAwait(false);
    }

    [Benchmark(Description = "Default-capacity executor")]
    public async Task DefaultCapacityExecutor()
    {
        await _taskExecutor.ExecuteAsync(SubjectMethod).ConfigureAwait(false);
    }

    static async Task SubjectMethod()
    {
        await SubjectChildMethod<long>().ConfigureAwait(false);
    }

    static async Task SubjectChildMethod<T>()
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
    public async Task RegularMethod()
    {
        await Parallel.ForAsync(0, 10, async (n, token) => await SubjectMethod());
    }

    [Benchmark(Description = "Tight-capacity executor", OperationsPerInvoke = 10)]
    public async Task TightCapacityExecutor()
    {
        await Parallel.ForAsync(0, 10,
            async (n, token) => await _tightCapacityExecutor.ExecuteAsync(SubjectMethod, token));
    }

    [Benchmark(Description = "Default-capacity executor", OperationsPerInvoke = 10)]
    public async Task DefaultCapacityExecutor()
    {
        await Parallel.ForAsync(0, 10,
            async (n, token) => await _taskExecutor.ExecuteAsync(SubjectMethod, token));
    }

    static async Task SubjectMethod()
    {
        await SubjectChildMethod<long>().ConfigureAwait(false);
    }

    static async Task SubjectChildMethod<T>()
    {
    }

    static async ValueTask SubjectMethodValue()
    {
        await SubjectChildMethodValue<long>().ConfigureAwait(false);
    }

    static async ValueTask SubjectChildMethodValue<T>()
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

    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new())
    {
        return _source.WaitToReadAsync(cancellationToken);
    }
}
