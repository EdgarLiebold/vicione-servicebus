using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedClientFactoryContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-client-context-and-request-endpoint-constructor-boundaries")]
    public void Constructors_RejectEveryMissingDependency()
    {
        var innerContext = new RecordingClientFactoryContext();
        using var clientFactory = new ClientFactoryLease(innerContext);
        var endpoint = new RecordingRequestSendEndpoint();
        var serviceProvider = new NullServiceProvider();

        Assert.Equal("clientFactory", Assert.Throws<ArgumentNullException>(() =>
            new ScopedClientFactoryContext(null!, serviceProvider)).ParamName);
        Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedClientFactoryContext(clientFactory.Factory, null!)).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            new ScopedRequestSendEndpoint<Probe>(null!, serviceProvider)).ParamName);
        Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedRequestSendEndpoint<Probe>(endpoint, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-client-context-forwards-state-pipes-and-endpoint-resolution")]
    public void Context_ForwardsStatePipesAndBothEndpointResolutionShapes()
    {
        var innerContext = new RecordingClientFactoryContext();
        using var clientFactory = new ClientFactoryLease(innerContext);
        var serviceProvider = new NullServiceProvider();
        var context = new ScopedClientFactoryContext(clientFactory.Factory, serviceProvider);
        IPipe<ConsumeContext<Probe>> consumePipe = Pipe.Empty<ConsumeContext<Probe>>();
        IPipe<ConsumeContext<Probe>> requestPipe = Pipe.Empty<ConsumeContext<Probe>>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, PassiveProxy>();
        Guid requestId = NewId.NextGuid();
        Uri destinationAddress = new("loopback://localhost/scoped-client-context");

        Assert.Equal(innerContext.DefaultTimeout, context.DefaultTimeout);
        Assert.Same(innerContext.TimeProvider, context.TimeProvider);
        Assert.Same(innerContext.MessageRoutes, context.MessageRoutes);
        Assert.Same(innerContext.ResponseAddress, context.ResponseAddress);
        Assert.Same(innerContext.ConsumeHandle,
            context.ConnectConsumePipe(consumePipe));
        Assert.Same(innerContext.ConsumeOptionsHandle,
            context.ConnectConsumePipe(consumePipe, ConnectPipeOptions.All));
        Assert.Same(innerContext.RequestHandle,
            context.ConnectRequestPipe(requestId, requestPipe));

        IRequestSendEndpoint<Probe> routed = context.GetRequestEndpoint<Probe>(consumeContext);
        IRequestSendEndpoint<Probe> addressed = context.GetRequestEndpoint<Probe>(destinationAddress, consumeContext);

        Assert.Equal(
            [(consumePipe, null), (consumePipe, ConnectPipeOptions.All)],
            innerContext.ConsumeConnections);
        Assert.Equal((requestId, requestPipe), Assert.Single(innerContext.RequestConnections));
        Assert.Equal(
            [(null, consumeContext), (destinationAddress, consumeContext)],
            innerContext.Resolutions);
        AssertScopedEndpoint(routed, innerContext.Endpoint, serviceProvider);
        AssertScopedEndpoint(addressed, innerContext.Endpoint, serviceProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-request-endpoint-forwards-both-send-shapes-with-scope-and-cancellation")]
    public async Task RequestEndpoint_ForwardsBothSendShapesWithScopeAndCancellationAsync()
    {
        var endpoint = new RecordingRequestSendEndpoint();
        var serviceProvider = new NullServiceProvider();
        var scoped = new ScopedRequestSendEndpoint<Probe>(endpoint, serviceProvider);
        IPipe<SendContext<Probe>> pipe = Pipe.Empty<SendContext<Probe>>();
        using var cancellation = new CancellationTokenSource();
        Guid initializedRequestId = NewId.NextGuid();
        Guid typedRequestId = NewId.NextGuid();
        object values = new { Value = "initialized" };
        var message = new Probe("typed");

        Probe initialized = await scoped.SendAsync(initializedRequestId, values, pipe, cancellation.Token);
        await scoped.SendAsync(typedRequestId, message, pipe, cancellation.Token);

        Assert.Same(endpoint.InitializedMessage, initialized);
        Assert.Equal(initializedRequestId, endpoint.InitializedRequestId);
        Assert.Same(values, endpoint.Values);
        Assert.Equal(cancellation.Token, endpoint.InitializedCancellationToken);
        Assert.Equal(typedRequestId, endpoint.TypedRequestId);
        Assert.Same(message, endpoint.TypedMessage);
        Assert.Equal(cancellation.Token, endpoint.TypedCancellationToken);
        AssertScopedPipe(endpoint.InitializedPipe, pipe, serviceProvider);
        AssertScopedPipe(endpoint.TypedPipe, pipe, serviceProvider);
    }

    private static void AssertScopedEndpoint(
        IRequestSendEndpoint<Probe> actual,
        IRequestSendEndpoint<Probe> endpoint,
        IServiceProvider serviceProvider)
    {
        var scoped = Assert.IsType<ScopedRequestSendEndpoint<Probe>>(actual);
        FieldInfo endpointField = typeof(ScopedRequestSendEndpoint<Probe>).GetField(
            "_endpoint",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo serviceProviderField = typeof(ScopedRequestSendEndpoint<Probe>).GetField(
            "_serviceProvider",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Same(endpoint, endpointField.GetValue(scoped));
        Assert.Same(serviceProvider, serviceProviderField.GetValue(scoped));
    }

    private static void AssertScopedPipe(
        IPipe<SendContext<Probe>>? actual,
        IPipe<SendContext<Probe>> pipe,
        IServiceProvider serviceProvider)
    {
        var scoped = Assert.IsType<ScopedSendPipeAdapter<Probe>>(actual);
        FieldInfo providerField = typeof(ScopedSendPipeAdapter<Probe>).GetField(
            "_provider",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo pipeField = typeof(SendContextPipeAdapter<Probe>).GetField(
            "_pipe",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Same(serviceProvider, providerField.GetValue(scoped));
        Assert.Same(pipe, pipeField.GetValue(scoped));
    }

    private sealed record Probe(string Value = "probe");

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class ClientFactoryLease : IDisposable
    {
        public ClientFactoryLease(ClientFactoryContext context)
        {
            Factory = new ClientFactory(context);
        }

        public ClientFactory Factory { get; }

        public void Dispose() => Factory.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class RecordingClientFactoryContext : ClientFactoryContext
    {
        public RecordingClientFactoryContext()
        {
            MessageRoutes = new MessageRouteTable();
        }

        public RequestTimeout DefaultTimeout { get; } = new(TimeSpan.FromSeconds(19));
        public TimeProvider TimeProvider { get; } = TimeProvider.System;
        public IMessageRouteTable MessageRoutes { get; }
        public Uri ResponseAddress { get; } = new("loopback://localhost/scoped-client-response");
        public RecordingRequestSendEndpoint Endpoint { get; } = new();
        public ConnectHandle ConsumeHandle { get; } = new EmptyConnectHandle();
        public ConnectHandle ConsumeOptionsHandle { get; } = new EmptyConnectHandle();
        public ConnectHandle RequestHandle { get; } = new EmptyConnectHandle();
        public List<(object Pipe, ConnectPipeOptions? Options)> ConsumeConnections { get; } = [];
        public List<(Guid RequestId, object Pipe)> RequestConnections { get; } = [];
        public List<(Uri? DestinationAddress, ConsumeContext? ConsumeContext)> Resolutions { get; } = [];

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            ConsumeConnections.Add((pipe, null));
            return ConsumeHandle;
        }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class
        {
            ConsumeConnections.Add((pipe, options));
            return ConsumeOptionsHandle;
        }

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            RequestConnections.Add((requestId, pipe));
            return RequestHandle;
        }

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class
        {
            Resolutions.Add((null, consumeContext));
            return CastEndpoint<T>();
        }

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class
        {
            Resolutions.Add((destinationAddress, consumeContext));
            return CastEndpoint<T>();
        }

        private IRequestSendEndpoint<T> CastEndpoint<T>()
            where T : class
        {
            Assert.Equal(typeof(Probe), typeof(T));
            return (IRequestSendEndpoint<T>)(object)Endpoint;
        }
    }

    private sealed class RecordingRequestSendEndpoint : IRequestSendEndpoint<Probe>
    {
        public Probe InitializedMessage { get; } = new("initialized-result");
        public Guid InitializedRequestId { get; private set; }
        public object? Values { get; private set; }
        public IPipe<SendContext<Probe>>? InitializedPipe { get; private set; }
        public CancellationToken InitializedCancellationToken { get; private set; }
        public Guid TypedRequestId { get; private set; }
        public Probe? TypedMessage { get; private set; }
        public IPipe<SendContext<Probe>>? TypedPipe { get; private set; }
        public CancellationToken TypedCancellationToken { get; private set; }

        public Task<Probe> SendAsync(
            Guid requestId,
            object values,
            IPipe<SendContext<Probe>> pipe,
            CancellationToken cancellationToken)
        {
            InitializedRequestId = requestId;
            Values = values;
            InitializedPipe = pipe;
            InitializedCancellationToken = cancellationToken;
            return Task.FromResult(InitializedMessage);
        }

        public Task SendAsync(
            Guid requestId,
            Probe message,
            IPipe<SendContext<Probe>> pipe,
            CancellationToken cancellationToken)
        {
            TypedRequestId = requestId;
            TypedMessage = message;
            TypedPipe = pipe;
            TypedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}
