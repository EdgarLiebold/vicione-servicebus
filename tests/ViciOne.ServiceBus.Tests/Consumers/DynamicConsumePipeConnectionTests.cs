using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class DynamicConsumePipeConnectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONSUMER-CONNECTION", "factory-delivers-exact-message-values")]
    public async Task ConsumerFactoryConnection_DeliversTheExactMessageValuesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var consumer = new SingleMessageConsumer();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        IHostReceiveEndpointHandle endpoint = await ConnectEndpointAsync(harness, timeout, cancellationToken);
        try
        {
            using ConnectHandle handle = endpoint.ReceiveEndpoint.ConnectConsumer(
                new InstanceConsumerFactory<SingleMessageConsumer>(consumer));
            var message = new FirstMessage(NewId.NextGuid());
            ISendEndpoint sendEndpoint = await harness.Bus.GetSendEndpointAsync(endpoint.ReceiveEndpoint.InputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            await sendEndpoint.SendAsync(message, cancellationToken).WaitAsync(timeout, cancellationToken);

            ConsumeContext<FirstMessage> observed = await consumer.Consumed.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(message, observed.Message);
            Assert.Equal(1, consumer.ConsumeCount);
        }
        finally
        {
            await endpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONSUMER-CONNECTION", "object-instance-multiple-contracts")]
    public async Task ObjectInstanceConnection_DeliversEveryImplementedMessageContractExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var consumer = new MultipleMessageConsumer();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        IHostReceiveEndpointHandle endpoint = await ConnectEndpointAsync(harness, timeout, cancellationToken);
        try
        {
            using ConnectHandle handle = endpoint.ReceiveEndpoint.ConnectInstance((object)consumer);
            var first = new FirstMessage(NewId.NextGuid());
            var second = new SecondMessage(NewId.NextGuid());
            ISendEndpoint sendEndpoint = await harness.Bus.GetSendEndpointAsync(endpoint.ReceiveEndpoint.InputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            await sendEndpoint.SendAsync(first, cancellationToken).WaitAsync(timeout, cancellationToken);
            await sendEndpoint.SendAsync(second, cancellationToken).WaitAsync(timeout, cancellationToken);

            FirstMessage observedFirst = await consumer.First.Task.WaitAsync(timeout, cancellationToken);
            SecondMessage observedSecond = await consumer.Second.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(first, observedFirst);
            Assert.Equal(second, observedSecond);
            Assert.Equal(1, consumer.FirstCount);
            Assert.Equal(1, consumer.SecondCount);
        }
        finally
        {
            await endpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-HANDLER-CONNECTION", "exact-message-values")]
    public async Task HandlerConnection_DeliversTheExactMessageValuesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var received = NewSignal<ConsumeContext<FirstMessage>>();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        IHostReceiveEndpointHandle endpoint = await ConnectEndpointAsync(harness, timeout, cancellationToken);
        try
        {
            using ConnectHandle handle = endpoint.ReceiveEndpoint.ConnectHandler<FirstMessage>(context =>
            {
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
            var message = new FirstMessage(NewId.NextGuid());
            ISendEndpoint sendEndpoint = await harness.Bus.GetSendEndpointAsync(endpoint.ReceiveEndpoint.InputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            await sendEndpoint.SendAsync(message, cancellationToken).WaitAsync(timeout, cancellationToken);

            ConsumeContext<FirstMessage> observed = await received.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(message, observed.Message);
        }
        finally
        {
            await endpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONSUMER-CONNECTION", "disconnect-stops-future-delivery")]
    public async Task Disconnect_PreventsFutureDeliveryAfterTheCurrentDispatchHasCompletedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var consumer = new SingleMessageConsumer();
        var receiveBarrier = new ReceiveCompletionObserver();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        using ConnectHandle observerHandle = harness.Bus.ConnectReceiveObserver(receiveBarrier);
        IHostReceiveEndpointHandle endpoint = await ConnectEndpointAsync(harness, timeout, cancellationToken);
        try
        {
            using ConnectHandle consumerHandle = endpoint.ReceiveEndpoint.ConnectConsumer(
                new InstanceConsumerFactory<SingleMessageConsumer>(consumer));
            ISendEndpoint sendEndpoint = await harness.Bus.GetSendEndpointAsync(endpoint.ReceiveEndpoint.InputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            await sendEndpoint.SendAsync(new FirstMessage(NewId.NextGuid()), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await receiveBarrier.First.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(1, consumer.ConsumeCount);

            consumerHandle.Disconnect();
            await sendEndpoint.SendAsync(new FirstMessage(NewId.NextGuid()), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await receiveBarrier.Second.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(1, consumer.ConsumeCount);
        }
        finally
        {
            await endpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-HANDLER-CONNECTION", "null-public-boundaries")]
    public void HandlerConnection_RejectsNullPublicArgumentsAtTheBoundary()
    {
        IConsumePipe pipe = new ConsumePipeSpecification().BuildConsumePipe();
        MessageHandler<FirstMessage> handler = _ => Task.CompletedTask;
        var pipeConfigurator = new PipeConfigurator<ConsumeContext<FirstMessage>>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            HandlerExtensions.Handler<FirstMessage>(null!, handler)).ParamName);
        Assert.Equal("connector", Assert.Throws<ArgumentNullException>(() =>
            HandlerExtensions.ConnectHandler(null!, handler)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
            pipe.ConnectHandler<FirstMessage>(null!)).ParamName);
        Assert.Equal("connector", Assert.Throws<ArgumentNullException>(() =>
            HandlerExtensions.ConnectRequestHandler<FirstMessage>(null!, Guid.Empty, handler, pipeConfigurator)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
            pipe.ConnectRequestHandler(Guid.Empty, null!, pipeConfigurator)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            pipe.ConnectRequestHandler(Guid.Empty, handler, null!)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
            new HandlerPipeSpecification<FirstMessage>(null!)).ParamName);

        var specification = new HandlerPipeSpecification<FirstMessage>(handler);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() =>
            ((IPipeSpecification<ConsumeContext<FirstMessage>>)specification).Apply(null!)).ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"dynamic-connections-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static async Task<IHostReceiveEndpointHandle> ConnectEndpointAsync(
        InMemoryTestHarness harness,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        IHostReceiveEndpointHandle endpoint = harness.Bus.ConnectReceiveEndpoint(
            $"dynamic-{NewId.NextGuid():N}",
            _ => { });
        await endpoint.Ready.WaitAsync(timeout, cancellationToken);
        return endpoint;
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record FirstMessage(Guid Id);

    public sealed record SecondMessage(Guid Id);

    private sealed class SingleMessageConsumer : IConsumer<FirstMessage>
    {
        public TaskCompletionSource<ConsumeContext<FirstMessage>> Consumed { get; } = NewSignal<ConsumeContext<FirstMessage>>();

        public int ConsumeCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<FirstMessage> context)
        {
            ConsumeCount++;
            Consumed.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    private sealed class MultipleMessageConsumer : IConsumer<FirstMessage>, IConsumer<SecondMessage>
    {
        public TaskCompletionSource<FirstMessage> First { get; } = NewSignal<FirstMessage>();

        public TaskCompletionSource<SecondMessage> Second { get; } = NewSignal<SecondMessage>();

        public int FirstCount { get; private set; }

        public int SecondCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<FirstMessage> context)
        {
            FirstCount++;
            First.TrySetResult(context.Message);
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(ConsumeContext<SecondMessage> context)
        {
            SecondCount++;
            Second.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class ReceiveCompletionObserver : IReceiveObserver
    {
        private int _receiveCount;

        public TaskCompletionSource<bool> First { get; } = NewSignal<bool>();

        public TaskCompletionSource<bool> Second { get; } = NewSignal<bool>();

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceiveAsync(ReceiveContext context)
        {
            switch (Interlocked.Increment(ref _receiveCount))
            {
                case 1:
                    First.TrySetResult(true);
                    break;
                case 2:
                    Second.TrySetResult(true);
                    break;
            }

            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception) => Task.CompletedTask;
    }
}
