using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Clients.Contexts;
using ViciOne.ServiceBus.Clients.Endpoints;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ReceiveEndpointClientFactoryContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RESPONSE-ENDPOINT", "context-projects-endpoint-contract-and-connections")]
    public void Context_ProjectsEndpointStateAndForwardsEveryResponseConnection()
    {
        var endpoint = new RecordingReceiveEndpoint();
        var handle = new RecordingHostReceiveEndpointHandle(endpoint);
        var clock = new FakeTimeProvider(new DateTimeOffset(2043, 4, 5, 6, 7, 8, TimeSpan.Zero));
        var timeout = new RequestTimeout(TimeSpan.FromSeconds(47));
        var context = new ReceiveEndpointClientFactoryContext(handle, timeout, clock);
        IPipe<ConsumeContext<ClientResponse>> pipe = Pipe.Empty<ConsumeContext<ClientResponse>>();
        Guid requestId = Guid.Parse("9c570000-0000-0000-0000-000000000077");

        ConnectHandle basicConnection = context.ConnectConsumePipe(pipe);
        ConnectHandle configuredConnection = context.ConnectConsumePipe(pipe, ConnectPipeOptions.All);
        ConnectHandle requestConnection = context.ConnectRequestPipe(requestId, pipe);

        Assert.Equal(endpoint.InputAddress, context.ResponseAddress);
        Assert.Equal(timeout, context.DefaultTimeout);
        Assert.Same(clock, context.TimeProvider);
        Assert.Throws<ConfigurationException>(() => _ = context.MessageRoutes);
        Assert.Same(endpoint.BasicConnection, basicConnection);
        Assert.Same(endpoint.ConfiguredConnection, configuredConnection);
        Assert.Same(endpoint.RequestConnection, requestConnection);
        Assert.Same(pipe, endpoint.BasicPipe);
        Assert.Same(pipe, endpoint.ConfiguredPipe);
        Assert.Equal(ConnectPipeOptions.All, endpoint.ConnectOptions);
        Assert.Equal(requestId, endpoint.RequestId);
        Assert.Same(pipe, endpoint.RequestPipe);

        var defaultContext = new ReceiveEndpointClientFactoryContext(handle);
        Assert.Equal(RequestTimeout.Default, defaultContext.DefaultTimeout);
        Assert.Same(TimeProvider.System, defaultContext.TimeProvider);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-REQUEST-RESPONSE-ENDPOINT", "readiness-gates-addressed-and-publish-resolution")]
    public async Task RequestEndpoints_WaitForReadinessAndPreserveResolutionAndSendArgumentsAsync(bool publish)
    {
        var endpoint = new RecordingReceiveEndpoint();
        var handle = new RecordingHostReceiveEndpointHandle(endpoint);
        var context = new ReceiveEndpointClientFactoryContext(handle);
        Uri destinationAddress = new("loopback://localhost/request-service");
        IRequestSendEndpoint<ClientRequest> requestEndpoint = publish
            ? context.GetRequestEndpoint<ClientRequest>()
            : context.GetRequestEndpoint<ClientRequest>(destinationAddress);
        Guid requestId = Guid.Parse("9c570000-0000-0000-0000-000000000078");
        var message = new ClientRequest("ready-gated");
        using var cancellation = new CancellationTokenSource();

        Task send = requestEndpoint.SendAsync(
            requestId,
            message,
            Pipe.Empty<SendContext<ClientRequest>>(),
            cancellation.Token);

        Assert.False(send.IsCompleted);
        Assert.Equal(0, endpoint.ResolutionCount);

        handle.CompleteReadiness();
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(1, endpoint.ResolutionCount);
        Assert.Equal(publish, endpoint.PublishWasResolved);
        Assert.Equal(publish ? null : destinationAddress, endpoint.DestinationAddress);
        Assert.Equal(cancellation.Token, endpoint.ResolutionCancellationToken);
        Assert.Equal(publish ? typeof(ClientRequest) : null, endpoint.PublishedMessageType);
        Assert.Equal(1, endpoint.SendEndpoint.SendCount);
        Assert.Same(message, endpoint.SendEndpoint.Message);
        Assert.Equal(cancellation.Token, endpoint.SendEndpoint.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RESPONSE-ENDPOINT", "owned-host-endpoint-stops-on-disposal")]
    public async Task HostContext_DisposalStopsItsOwnedEndpointAsync()
    {
        var endpoint = new RecordingReceiveEndpoint();
        var handle = new RecordingHostReceiveEndpointHandle(endpoint);
        var context = new HostReceiveEndpointClientFactoryContext(handle);

        await context.DisposeAsync();

        Assert.Equal(1, handle.StopCount);
        Assert.Equal(CancellationToken.None, handle.StopCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RESPONSE-ENDPOINT", "constructors-require-handle-and-address")]
    public void Constructors_RejectMissingEndpointDependencies()
    {
        var endpoint = new RecordingReceiveEndpoint();
        var handle = new RecordingHostReceiveEndpointHandle(endpoint);

        Assert.Equal("handle", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointClientFactoryContext(null!)).ParamName);
        Assert.Equal("handle", Assert.Throws<ArgumentNullException>(() =>
            new HostReceiveEndpointClientFactoryContext(null!)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointSendRequestSendEndpoint<ClientRequest>(handle, null!, null)).ParamName);
        Assert.Equal("handle", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointSendRequestSendEndpoint<ClientRequest>(null!, new Uri("loopback://localhost/request"), null)).ParamName);
        Assert.Equal("handle", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointPublishRequestSendEndpoint<ClientRequest>(null!, null)).ParamName);
    }

    private sealed record ClientRequest(string Value);

    private sealed record ClientResponse(string Value);

    private sealed class RecordingHostReceiveEndpointHandle(IReceiveEndpoint receiveEndpoint) : IHostReceiveEndpointHandle
    {
        private readonly TaskCompletionSource<ReceiveEndpointReady> _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReceiveEndpoint ReceiveEndpoint { get; } = receiveEndpoint;

        public Task<ReceiveEndpointReady> Ready => _ready.Task;

        public int StopCount { get; private set; }

        public CancellationToken StopCancellationToken { get; private set; }

        public void CompleteReadiness() => _ready.TrySetResult(new ReadyEvent(ReceiveEndpoint));

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;
            StopCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingReceiveEndpoint : IReceiveEndpoint
    {
        public Uri InputAddress { get; } = new("loopback://localhost/response-endpoint");

        public Task<ReceiveEndpointReady> Started =>
            Task.FromResult<ReceiveEndpointReady>(new ReadyEvent(this));

        public RecordingSendEndpoint SendEndpoint { get; } = new();

        public ConnectHandle BasicConnection { get; } = new EmptyConnectHandle();

        public ConnectHandle ConfiguredConnection { get; } = new EmptyConnectHandle();

        public ConnectHandle RequestConnection { get; } = new EmptyConnectHandle();

        public object? BasicPipe { get; private set; }

        public object? ConfiguredPipe { get; private set; }

        public object? RequestPipe { get; private set; }

        public ConnectPipeOptions ConnectOptions { get; private set; }

        public Guid RequestId { get; private set; }

        public int ResolutionCount { get; private set; }

        public bool PublishWasResolved { get; private set; }

        public Uri? DestinationAddress { get; private set; }

        public Type? PublishedMessageType { get; private set; }

        public CancellationToken ResolutionCancellationToken { get; private set; }

        public IReceiveEndpointHandle Start(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            BasicPipe = pipe;
            return BasicConnection;
        }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class
        {
            ConfiguredPipe = pipe;
            ConnectOptions = options;
            return ConfiguredConnection;
        }

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            RequestId = requestId;
            RequestPipe = pipe;
            return RequestConnection;
        }

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ResolutionCount++;
            PublishWasResolved = false;
            DestinationAddress = address;
            ResolutionCancellationToken = cancellationToken;
            return Task.FromResult<ISendEndpoint>(SendEndpoint);
        }

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            ResolutionCount++;
            PublishWasResolved = true;
            PublishedMessageType = typeof(T);
            ResolutionCancellationToken = cancellationToken;
            return Task.FromResult<ISendEndpoint>(SendEndpoint);
        }

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingSendEndpoint : IAdvancedSendEndpoint
    {
        public int SendCount { get; private set; }

        public object? Message { get; private set; }

        public SendOptions? Options { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<TMessage>(TMessage message, SendOptions options, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Record(message, cancellationToken);
            Options = options;
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default)
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        {
            Record(message, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
            where T : class
        {
            Record(values, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            Record(values, cancellationToken);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            Record(values, cancellationToken);
            return Task.CompletedTask;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();

        private void Record(object message, CancellationToken cancellationToken)
        {
            SendCount++;
            Message = message;
            CancellationToken = cancellationToken;
        }
    }

    private sealed record ReadyEvent(IReceiveEndpoint ReceiveEndpoint) : ReceiveEndpointReady
    {
        public Uri InputAddress => ReceiveEndpoint.InputAddress;

        public bool IsStarted => true;
    }
}
