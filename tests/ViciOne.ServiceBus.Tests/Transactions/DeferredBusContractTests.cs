using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using ViciOne.ServiceBus.Transactions;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transactions;

public sealed class DeferredBusContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "publish-overloads-reject-invalid-input-before-buffering")]
    public async Task PublishOverloads_RejectInvalidInputBeforeAnythingIsBufferedAsync()
    {
        var driver = new BufferedBusTestDriver();
        IBufferedBus bus = driver.Bus;
        CancellationToken token = TestContext.Current.CancellationToken;
        IPipe<PublishContext<DeferredMessage>> typedPipe = Pipe.Empty<PublishContext<DeferredMessage>>();
        IPipe<PublishContext> pipe = Pipe.Empty<PublishContext>();

        AssertParameter("message", () => bus.PublishAsync<DeferredMessage>(null!, token));
        AssertParameter("message", () => bus.PublishAsync<DeferredMessage>(null!, typedPipe, token));
        AssertParameter("pipe", () => bus.PublishAsync(new DeferredMessage(), (IPipe<PublishContext<DeferredMessage>>)null!, token));
        AssertParameter("message", () => bus.PublishAsync<DeferredMessage>(null!, pipe, token));
        AssertParameter("pipe", () => bus.PublishAsync(new DeferredMessage(), (IPipe<PublishContext>)null!, token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, pipe, token));
        AssertParameter("publishPipe", () => bus.Advanced().PublishAsync(
            new DeferredMessage(),
            (IPipe<PublishContext>)null!,
            token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, typeof(DeferredMessage), token));
        AssertParameter("messageType", () => bus.Advanced().PublishAsync(new DeferredMessage(), (Type)null!, token));
        AssertParameter("message", () => bus.Advanced().PublishAsync(null!, typeof(DeferredMessage), pipe, token));
        AssertParameter("messageType", () => bus.Advanced().PublishAsync(new DeferredMessage(), null!, pipe, token));
        AssertParameter("publishPipe", () => bus.Advanced().PublishAsync(new DeferredMessage(), typeof(DeferredMessage), null!, token));
        AssertParameter("values", () => bus.PublishAsync<DeferredMessage>((object)null!, token));
        AssertParameter("values", () => bus.PublishAsync<DeferredMessage>((object)null!, typedPipe, token));
        AssertParameter("pipe", () => bus.PublishAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<PublishContext<DeferredMessage>>)null!, token));
        AssertParameter("values", () => bus.PublishAsync<DeferredMessage>((object)null!, pipe, token));
        AssertParameter("pipe", () => bus.PublishAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<PublishContext>)null!, token));

        await bus.FlushAsync(token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "send-overloads-reject-invalid-input-before-buffering")]
    public async Task SendOverloads_RejectInvalidInputBeforeAnythingIsBufferedAsync()
    {
        var driver = new BufferedBusTestDriver();
        ITransportSendEndpoint endpoint = driver.CreateSendEndpoint();
        CancellationToken token = TestContext.Current.CancellationToken;
        IPipe<SendContext<DeferredMessage>> typedPipe = Pipe.Empty<SendContext<DeferredMessage>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        DeferredMessage missingMessage = null!;

        AssertParameter("message", () => endpoint.CreateSendContextAsync<DeferredMessage>(null!, typedPipe, token));
        AssertParameter("pipe", () => endpoint.CreateSendContextAsync(new DeferredMessage(), null!, token));
        AssertParameter("message", () => endpoint.SendAsync(missingMessage, token));
        AssertParameter("message", () => endpoint.SendAsync(missingMessage, typedPipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), (IPipe<SendContext<DeferredMessage>>)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(missingMessage, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), (IPipe<SendContext>)null!, token));
        AssertParameter("message", () => endpoint.SendAsync((object)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(null!, typeof(DeferredMessage), token));
        AssertParameter("messageType", () => endpoint.SendAsync(new DeferredMessage(), (Type)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(null!, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), (IPipe<SendContext>)null!, token));
        AssertParameter("message", () => endpoint.SendAsync(null!, typeof(DeferredMessage), pipe, token));
        AssertParameter("messageType", () => endpoint.SendAsync(new DeferredMessage(), null!, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync(new DeferredMessage(), typeof(DeferredMessage), null!, token));
        AssertParameter("values", () => endpoint.SendAsync<DeferredMessage>((object)null!, token));
        AssertParameter("values", () => endpoint.SendAsync<DeferredMessage>((object)null!, typedPipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<SendContext<DeferredMessage>>)null!, token));
        AssertParameter("values", () => endpoint.SendAsync<DeferredMessage>((object)null!, pipe, token));
        AssertParameter("pipe", () => endpoint.SendAsync<DeferredMessage>(new { Value = "valid" }, (IPipe<SendContext>)null!, token));

        await driver.Bus.FlushAsync(token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "send-endpoint-construction-boundaries")]
    public void SendEndpointAdapter_RejectsMissingAndNonTransportEndpoints()
    {
        var driver = new BufferedBusTestDriver();

        AssertParameter("endpoint", () => driver.WrapSendEndpoint(null!));
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            driver.WrapSendEndpoint(BufferedBusTestDriver.CreateNonTransportSendEndpoint()));

        Assert.Equal("endpoint", exception.ParamName);
        Assert.Contains("transport send operations", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "forwarded-bus-boundaries-reject-invalid-input-locally")]
    public void ForwardedBusMembers_RejectInvalidInputBeforeCallingTheInnerBus()
    {
        var driver = new BufferedBusTestDriver();
        IBufferedBus bus = driver.Bus;
        ITransportSendEndpoint endpoint = driver.CreateSendEndpoint();

        AssertParameter("observer", () => bus.ConnectPublishObserver(null!));
        AssertParameter("observer", () => bus.ConnectSendObserver(null!));
        AssertParameter("pipe", () => bus.ConnectConsumePipe<DeferredMessage>(null!));
        AssertParameter("pipe", () => bus.ConnectConsumePipe<DeferredMessage>(null!, default));
        AssertParameter("pipe", () => bus.ConnectRequestPipe<DeferredMessage>(Guid.NewGuid(), null!));
        AssertParameter("observer", () => bus.ConnectConsumeMessageObserver<DeferredMessage>(null!));
        AssertParameter("observer", () => bus.ConnectConsumeObserver(null!));
        AssertParameter("observer", () => bus.ConnectReceiveObserver(null!));
        AssertParameter("observer", () => bus.ConnectReceiveEndpointObserver(null!));
        AssertParameter("observer", () => bus.ConnectEndpointConfigurationObserver(null!));
        AssertParameter("definition", () => bus.ConnectReceiveEndpoint(null!, endpointNameFormatter: null));
        AssertParameter("queueName", () => bus.ConnectReceiveEndpoint((string)null!, configureEndpoint: null));
        AssertParameter("queueName", () => bus.ConnectReceiveEndpoint(" ", configureEndpoint: null), typeof(ArgumentException));
        AssertParameter("observer", () => endpoint.ConnectSendObserver(null!));

        ArgumentNullException probe = Assert.Throws<ArgumentNullException>(() => bus.Probe(null!));
        Assert.Equal("context", probe.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "forwarded-bus-members-preserve-arguments-and-results")]
    public async Task ForwardedBusMembers_PreserveEveryArgumentAndResultAsync()
    {
        IBus innerBus = DispatchProxy.Create<IBus, RecordingBusProxy>();
        var recorder = (RecordingBusProxy)(object)innerBus;
        var driver = new BufferedBusTestDriver(innerBus);
        IBufferedBus bus = driver.Bus;
        CancellationToken token = TestContext.Current.CancellationToken;
        IPublishObserver publishObserver = CreateUnused<IPublishObserver>();
        ISendObserver sendObserver = CreateUnused<ISendObserver>();
        IPipe<ConsumeContext<DeferredMessage>> pipe = Pipe.Empty<ConsumeContext<DeferredMessage>>();
        IConsumeMessageObserver<DeferredMessage> messageObserver = CreateUnused<IConsumeMessageObserver<DeferredMessage>>();
        IConsumeObserver consumeObserver = CreateUnused<IConsumeObserver>();
        IReceiveObserver receiveObserver = CreateUnused<IReceiveObserver>();
        IReceiveEndpointObserver endpointObserver = CreateUnused<IReceiveEndpointObserver>();
        IEndpointConfigurationObserver configurationObserver = CreateUnused<IEndpointConfigurationObserver>();
        IEndpointDefinition definition = CreateUnused<IEndpointDefinition>();
        IEndpointNameFormatter formatter = CreateUnused<IEndpointNameFormatter>();
        Action<IReceiveEndpointConfigurator> configure = static _ => { };
        ProbeContext probe = CreateUnused<ProbeContext>();
        var address = new Uri("loopback://localhost/deferred-contract");
        Guid requestId = NewId.NextGuid();

        Assert.Same(recorder.ConnectHandle, bus.ConnectPublishObserver(publishObserver));
        Assert.Same(recorder.ConnectHandle, bus.ConnectSendObserver(sendObserver));
        Assert.Same(recorder.ConnectHandle, bus.ConnectConsumePipe(pipe));
        Assert.Same(recorder.ConnectHandle, bus.ConnectConsumePipe(pipe, ConnectPipeOptions.ConfigureConsumeTopology));
        Assert.Same(recorder.ConnectHandle, bus.ConnectRequestPipe(requestId, pipe));
        Assert.Same(recorder.ConnectHandle, bus.ConnectConsumeMessageObserver(messageObserver));
        Assert.Same(recorder.ConnectHandle, bus.ConnectConsumeObserver(consumeObserver));
        Assert.Same(recorder.ConnectHandle, bus.ConnectReceiveObserver(receiveObserver));
        Assert.Same(recorder.ConnectHandle, bus.ConnectReceiveEndpointObserver(endpointObserver));
        Assert.Same(recorder.ConnectHandle, bus.ConnectEndpointConfigurationObserver(configurationObserver));
        Assert.Same(recorder.HostHandle, bus.ConnectReceiveEndpoint(definition, formatter, configure));
        Assert.Same(recorder.HostHandle, bus.ConnectReceiveEndpoint("deferred-contract", configure));
        bus.Probe(probe);
        Assert.Equal(recorder.Address, bus.Address);
        Assert.Same(recorder.Topology, bus.Topology);

        ISendEndpoint sendEndpoint = await bus.GetSendEndpointAsync(address, token);
        ISendEndpoint publishEndpoint = await bus.GetPublishSendEndpointAsync<DeferredMessage>(token);

        Assert.IsAssignableFrom<ITransportSendEndpoint>(sendEndpoint);
        Assert.IsAssignableFrom<ITransportSendEndpoint>(publishEndpoint);
        Assert.NotSame(recorder.TransportEndpoint, sendEndpoint);
        Assert.NotSame(recorder.TransportEndpoint, publishEndpoint);
        Assert.Same(
            recorder.TransportRecorder.ConnectHandle,
            sendEndpoint.ConnectSendObserver(sendObserver));
        Assert.Same(
            recorder.TransportRecorder.ConnectHandle,
            publishEndpoint.ConnectSendObserver(sendObserver));
        Assert.Equal(
            [
                "ConnectPublishObserver",
                "ConnectSendObserver",
                "ConnectConsumePipe",
                "ConnectConsumePipe",
                "ConnectRequestPipe",
                "ConnectConsumeMessageObserver",
                "ConnectConsumeObserver",
                "ConnectReceiveObserver",
                "ConnectReceiveEndpointObserver",
                "ConnectEndpointConfigurationObserver",
                "ConnectReceiveEndpoint",
                "ConnectReceiveEndpoint",
                "Probe",
                "get_Address",
                "get_Topology",
                "GetSendEndpointAsync",
                "GetPublishSendEndpointAsync",
            ],
            recorder.Invocations.Select(invocation => invocation.Method.Name));
        Assert.Same(publishObserver, recorder.Invocations.ElementAt(0).Arguments[0]);
        Assert.Same(sendObserver, recorder.Invocations.ElementAt(1).Arguments[0]);
        Assert.Same(pipe, recorder.Invocations.ElementAt(2).Arguments[0]);
        Assert.Equal(ConnectPipeOptions.ConfigureConsumeTopology, recorder.Invocations.ElementAt(3).Arguments[1]);
        Assert.Equal(requestId, recorder.Invocations.ElementAt(4).Arguments[0]);
        Assert.Same(definition, recorder.Invocations.ElementAt(10).Arguments[0]);
        Assert.Same(formatter, recorder.Invocations.ElementAt(10).Arguments[1]);
        Assert.Same(configure, recorder.Invocations.ElementAt(10).Arguments[2]);
        Assert.Equal("deferred-contract", recorder.Invocations.ElementAt(11).Arguments[0]);
        Assert.Same(probe, recorder.Invocations.ElementAt(12).Arguments[0]);
        Assert.Equal(address, recorder.Invocations.ElementAt(15).Arguments[0]);
        Assert.Equal(token, recorder.Invocations.ElementAt(15).Arguments[1]);
        Assert.Equal(token, recorder.Invocations.ElementAt(16).Arguments[0]);
        Assert.Equal(2, recorder.TransportRecorder.Invocations.Count);
        Assert.All(
            recorder.TransportRecorder.Invocations,
            invocation =>
            {
                Assert.Equal("ConnectSendObserver", invocation.Method.Name);
                Assert.Same(sendObserver, Assert.Single(invocation.Arguments));
            });
    }

    private static void AssertParameter(string expected, Func<object?> action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => action());
        Assert.Equal(expected, exception.ParamName);
    }

    private static void AssertParameter(string expected, Func<object?> action, Type exceptionType)
    {
        Exception exception = Assert.Throws(exceptionType, () => action());
        Assert.Equal(expected, Assert.IsAssignableFrom<ArgumentException>(exception).ParamName);
    }

    private static T CreateUnused<T>()
        where T : class => DispatchProxy.Create<T, UnexpectedInvocationProxy>();

    private sealed record Invocation(MethodInfo Method, object?[] Arguments);

    private class RecordingBusProxy : DispatchProxy
    {
        public RecordingBusProxy()
        {
            TransportEndpoint = DispatchProxy.Create<ITransportSendEndpoint, RecordingTransportEndpointProxy>();
            TransportRecorder = (RecordingTransportEndpointProxy)(object)TransportEndpoint;
        }

        public Uri Address { get; } = new("loopback://localhost/recording-bus");

        public ConnectHandle ConnectHandle { get; } = CreateUnused<ConnectHandle>();

        public IHostReceiveEndpointHandle HostHandle { get; } = CreateUnused<IHostReceiveEndpointHandle>();

        public ConcurrentQueue<Invocation> Invocations { get; } = new();

        public IBusTopology Topology { get; } = CreateUnused<IBusTopology>();

        public ITransportSendEndpoint TransportEndpoint { get; }

        public RecordingTransportEndpointProxy TransportRecorder { get; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The intercepted bus method is required.");
            Invocations.Enqueue(new Invocation(method, args?.ToArray() ?? []));

            if (method.ReturnType == typeof(ConnectHandle))
                return ConnectHandle;
            if (method.ReturnType == typeof(IHostReceiveEndpointHandle))
                return HostHandle;
            if (method.ReturnType == typeof(Uri))
                return Address;
            if (method.ReturnType == typeof(IBusTopology))
                return Topology;
            if (method.ReturnType == typeof(Task<ISendEndpoint>))
                return Task.FromResult<ISendEndpoint>(TransportEndpoint);
            if (method.ReturnType == typeof(void))
                return null;

            throw new InvalidOperationException($"Unexpected bus return type '{method.ReturnType}'.");
        }
    }

    private class RecordingTransportEndpointProxy : DispatchProxy
    {
        public ConnectHandle ConnectHandle { get; } = CreateUnused<ConnectHandle>();

        public ConcurrentQueue<Invocation> Invocations { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The intercepted endpoint method is required.");
            Invocations.Enqueue(new Invocation(method, args?.ToArray() ?? []));

            if (method.Name == "ConnectSendObserver" && method.ReturnType == typeof(ConnectHandle))
                return ConnectHandle;

            throw new InvalidOperationException($"Unexpected endpoint member '{method.Name}'.");
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The test double member '{targetMethod?.Name}' was not expected to be called.");
    }

    private sealed record DeferredMessage(string Value = "value");
}
