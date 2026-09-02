namespace ViciOne.ServiceBus.BenchmarkConsole;

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Util;


/// <summary>
/// Measures the complete request/response path over the InMemory transport. It is intentionally separate from the
/// direct-versus-mediator dispatch comparison because those operations do not have equivalent semantics.
/// </summary>
[MemoryDiagnoser]
public class InMemoryRequestResponseBenchmark
{
    IBusControl _busControl;
    IRequestClient<ExampleRequest> _requestClient;
    ExampleRequest _request;

    [GlobalSetup]
    public void Setup()
    {
        _busControl = Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.ReceiveEndpoint("input-queue", x => x.Consumer<ExampleRequestConsumer>());
        });

        TaskBlocking.Wait(() => _busControl.StartAsync(CancellationToken.None));
        _requestClient = _busControl.CreateRequestClient<ExampleRequest>();
        _request = new ExampleRequest
        {
            Name = "Frank",
            Amount = 123.45m
        };
    }

    [GlobalCleanup]
    public Task Cleanup() => _busControl.StopAsync(CancellationToken.None);

    [Benchmark(Description = "InMemory request/response")]
    public Task<Response<ExampleResponse>> RequestResponse() => _requestClient.GetResponse<ExampleResponse>(_request);
}


public class ExampleRequestConsumer :
    IConsumer<ExampleRequest>
{
    public Task Consume(ConsumeContext<ExampleRequest> context)
    {
        return context.RespondAsync(new ExampleResponse
        {
            Name = context.Message.Name,
            Amount = context.Message.Amount
        });
    }
}


public class ExampleRequest
{
    public string Name { get; set; }
    public decimal Amount { get; set; }
}


public class ExampleResponse
{
    public string Name { get; set; }
    public decimal Amount { get; set; }
}
