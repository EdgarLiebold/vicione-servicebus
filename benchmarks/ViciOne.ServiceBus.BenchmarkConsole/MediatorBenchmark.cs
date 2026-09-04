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
    public Task CallingHandlerDirectly() => _handler.Handle(_command, CancellationToken.None);

    [Benchmark(Description = "ViciOne.ServiceBus mediator call")]
    public Task CallingHandlerWithViciOneServiceBusMediator() => _mediator.Send(_command, CancellationToken.None);
}


public class ExampleCommandHandler :
    IConsumer<ExampleCommand>
{
    public Task Consume(ConsumeContext<ExampleCommand> context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Handle(ExampleCommand request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
