using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.BenchmarkConsole.Throughput;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser]
public class SendBenchmark
{
    readonly SetConcurrencyLimit _command = new Command(32);
    readonly IPipe<TestContext> _concurrencyPipe;
    readonly TestContext _context;
    readonly IPipe<PipeContext> _dispatchPipe;
    readonly IPipe<PipeContext> _doubleDispatchPipe;
    readonly IPipe<TestContext> _doublePipe;
    readonly IPipe<TestContext> _emptyPipe;
    readonly IPipe<TestContext> _faultPipe;
    readonly IPipe<TestContext> _retryPipe;
    readonly IPipe<PipeContext> _tripleDispatchPipe;

    public SendBenchmark()
    {
        _context = new ThroughputTestContext(Guid.NewGuid(), "Payload");

        _emptyPipe = Pipe.Empty<TestContext>();

        _retryPipe = Pipe.New<TestContext>(x =>
        {
            x.UseRetry(r => r.Immediate(1));

            x.UseFilter(new BenchmarkFilter());
        });

        _concurrencyPipe = Pipe.New<TestContext>(x =>
        {
            x.UseConcurrencyLimit(Environment.ProcessorCount);

            x.UseFilter(new BenchmarkFilter());
        });

        _doublePipe = Pipe.New<TestContext>(x =>
        {
            x.UseConcurrencyLimit(Environment.ProcessorCount);
            x.UseRetry(r => r.Immediate(1));

            x.UseFilter(new BenchmarkFilter());
        });

        _faultPipe = Pipe.New<TestContext>(x =>
        {
            x.UseRetry(r => r.Immediate(1));

            x.UseFilter(new FaultFilter());
        });

        var dispatchPipe = new PipeRouter();
        _dispatchPipe = dispatchPipe;

        dispatchPipe.ConnectPipe(Pipe.Empty<CommandContext<SetConcurrencyLimit>>());

        var doubleDispatchPipe = new PipeRouter();
        _doubleDispatchPipe = doubleDispatchPipe;

        doubleDispatchPipe.ConnectPipe(Pipe.Empty<CommandContext<SetConcurrencyLimit>>());
        doubleDispatchPipe.ConnectPipe(Pipe.Empty<CommandContext<SetRateLimit>>());

        var tripleDispatchPipe = new PipeRouter();
        _tripleDispatchPipe = tripleDispatchPipe;

        tripleDispatchPipe.ConnectPipe(Pipe.Empty<CommandContext<SetConcurrencyLimit>>());
        tripleDispatchPipe.ConnectPipe(Pipe.Empty<CommandContext<SetRateLimit>>());
        tripleDispatchPipe.ConnectPipe(Pipe.Empty<CommandContext<BusReady>>());
    }

    [Benchmark]
    public async Task EmptyPipeAsync()
    {
        await _emptyPipe.SendAsync(_context);
    }

    [Benchmark]
    public async Task RetryPipeAsync()
    {
        await _retryPipe.SendAsync(_context);
    }

    [Benchmark]
    public async Task ConcurrencyPipeAsync()
    {
        await _concurrencyPipe.SendAsync(_context);
    }

    [Benchmark]
    public async Task DoublePipeAsync()
    {
        await _doublePipe.SendAsync(_context);
    }

    [Benchmark]
    public async Task DispatchPipeAsync()
    {
        await _dispatchPipe.SendCommandAsync(_command);
    }

    [Benchmark]
    public async Task DoubleDispatchPipeAsync()
    {
        await _doubleDispatchPipe.SendCommandAsync(_command);
    }

    [Benchmark]
    public async Task TripleDispatchPipeAsync()
    {
        await _tripleDispatchPipe.SendCommandAsync(_command);
    }

    public async Task FaultPipeAsync()
    {
        try
        {
            await _faultPipe.SendAsync(_context);
        }
        catch
        {
        }
    }


    class Command :
        SetConcurrencyLimit
    {
        public Command(int concurrencyLimit)
        {
            ConcurrencyLimit = concurrencyLimit;
        }

        public DateTimeOffset? Timestamp { get; } = TimeProvider.System.GetUtcNow();
        public string? Id => null;
        public int ConcurrencyLimit { get; }
    }
}
