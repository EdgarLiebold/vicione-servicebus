namespace ViciOne.ServiceBus.BenchmarkConsole
{
    using System.Threading;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Util;


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
        IBusControl _busControl;
        ExampleCommandHandler _handler;
        ViciOne.ServiceBus.Mediator.IMediator _mediator;
        IRequestClient<ExampleRequest> _requestClient;

        [GlobalSetup]
        public void Setup()
        {
            _mediator = Bus.Factory.CreateMediator(cfg =>
            {
                cfg.Consumer<ExampleCommandHandler>();
            });

            _busControl = Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.ReceiveEndpoint("input-queue", x => x.Consumer<ExampleRequestConsumer>());
            });

            TaskUtil.Await(() => _busControl.StartAsync(CancellationToken.None));

            _requestClient = _busControl.CreateRequestClient<ExampleRequest>();

            _handler = new ExampleCommandHandler();
        }

        [GlobalCleanup]
        public Task Cleanup()
        {
            return _busControl.StopAsync(CancellationToken.None);
        }

        [Benchmark(Description = "Direct")]
        public async Task CallingHandler_Directly()
        {
            var command = new ExampleCommand("Example Arg", 2);
            await _handler.Handle(command, CancellationToken.None);
        }

        [Benchmark(Description = "ViciOne.ServiceBus")]
        public async Task CallingHandler_WithViciOneServiceBusMediator()
        {
            var command = new ExampleCommand("Example Arg", 2);
            await _mediator.Send(command, CancellationToken.None);
        }

        [Benchmark(Description = "InMemoryBus")]
        public async Task CallingHandler_WithViciOneServiceBusInMemoryBus()
        {
            var request = new ExampleRequest
            {
                Name = "Frank",
                Amount = 123.45m
            };
            await _requestClient.GetResponse<ExampleResponse>(request);
        }
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
}
