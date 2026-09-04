using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace ViciOne.ServiceBus.BenchmarkConsole;

public class ExampleCommand
{
    public ExampleCommand(string arg1, int arg2)
    {
        Arg1 = arg1;
        Arg2 = arg2;
    }

    public string Arg1 { get; }

    public int Arg2 { get; }
}


[MemoryDiagnoser]
public class MediatorBenchmark
{
    ExampleCommand _command;
    ExampleCommandHandler _handler;
    ViciOne.ServiceBus.Mediator.IMediator _mediator;

    [GlobalSetup]
    public void Setup()
    {
        _mediator = Bus.Factory.CreateMediator(cfg =>
        {
            cfg.Consumer<ExampleCommandHandler>();
        });

        _command = new ExampleCommand("Example Arg", 2);
        _handler = new ExampleCommandHandler();
    }

    [Benchmark(Baseline = true, Description = "Direct handler call")]
    public Task CallingHandlerDirectlyAsync() => _handler.HandleAsync(_command, CancellationToken.None);

    [Benchmark(Description = "ViciOne.ServiceBus mediator call")]
    public Task CallingHandlerWithViciOneServiceBusMediatorAsync() => _mediator.SendAsync(_command, CancellationToken.None);
}


public class ExampleCommandHandler :
    IConsumer<ExampleCommand>
{
    public Task ConsumeAsync(ConsumeContext<ExampleCommand> context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <param name="request">The request used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task HandleAsync(ExampleCommand request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
