namespace ViciOne.ServiceBus.BenchmarkConsole;

using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Mediator;


/// <summary>
/// The throughput of the mediator of this product, moved here from two NUnit cases that pushed two hundred thousand
/// messages through it with a stopwatch and wrote messages per second to the console.
///
/// Both scenarios use this product only. They deliberately do not reference the external mediator package that the
/// component contract removes, so the retained benchmark identity does not depend on a decided removal.
/// </summary>
[MemoryDiagnoser]
public class MediatorThroughputBenchmark
{
    IRequestClient<BenchmarkRequest> _client;
    IMediator _mediator;
    IMediator _responder;

    /// <summary>
    /// The number of messages that are in flight at the same time, which is what the NUnit cases called the split.
    /// </summary>
    [Params(1, 20)]
    public int Concurrency { get; set; }

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
    }

    [Benchmark(Description = "Mediator send")]
    public Task Send()
    {
        var message = new BenchmarkCommand();

        return Task.WhenAll(Enumerable.Range(0, Concurrency).Select(_ => _mediator.Send(message)));
    }

    [Benchmark(Description = "Mediator request client")]
    public Task RequestClient()
    {
        var message = new BenchmarkRequest();

        return Task.WhenAll(Enumerable.Range(0, Concurrency)
            .Select(_ => _client.GetResponse<BenchmarkResponse>(message)));
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
