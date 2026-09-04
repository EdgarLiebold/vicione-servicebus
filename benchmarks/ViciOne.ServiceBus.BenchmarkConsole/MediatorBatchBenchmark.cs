using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.BenchmarkConsole;
/// <summary>
/// Measures completion time and allocation cost for a concurrently submitted mediator batch. BenchmarkDotNet reports
/// one complete batch as one operation; the result is not a per-message throughput figure.
/// </summary>
[MemoryDiagnoser]
public class MediatorBatchBenchmark
{
    BenchmarkCommand _command;
    IRequestClient<BenchmarkRequest> _client;
    IMediator _mediator;
    BenchmarkRequest _request;
    IMediator _responder;

    [Params(1, 20)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = Bus.Factory.CreateMediator(cfg =>
        {
            cfg.Handler<BenchmarkCommand>(context => Task.CompletedTask);
        });

        _responder = Bus.Factory.CreateMediator(cfg =>
        {
            cfg.Handler<BenchmarkRequest>(context => context.RespondAsync(new BenchmarkResponse()));
        });

        _client = _responder.CreateRequestClient<BenchmarkRequest>();
        _command = new BenchmarkCommand();
        _request = new BenchmarkRequest();
    }

    [Benchmark(Description = "Mediator send batch completion")]
    public Task SendBatch()
    {
        return Task.WhenAll(Enumerable.Range(0, BatchSize).Select(_ => _mediator.Send(_command)));
    }

    [Benchmark(Description = "Mediator request batch completion")]
    public Task RequestBatch()
    {
        return Task.WhenAll(Enumerable.Range(0, BatchSize)
            .Select(_ => _client.GetResponse<BenchmarkResponse>(_request)));
    }


    public class BenchmarkCommand
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
    }


    public class BenchmarkRequest
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
    }


    public class BenchmarkResponse
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
    }
}
