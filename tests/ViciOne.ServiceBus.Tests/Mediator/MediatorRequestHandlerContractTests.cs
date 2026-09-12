using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorRequestHandlerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-HANDLER", "one-way-message-and-cancellation-token")]
    public async Task OneWayHandler_ReceivesTheExactRequestAndCancellationTokenAsync()
    {
        var handler = new RecordingOneWayHandler();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Consumer(() => handler);
        });
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var request = new OneWayRequest(NewId.NextGuid(), "one-way");

        await mediator.SendAsync(request, source.Token);

        Assert.Same(request, handler.Request);
        Assert.Equal(source.Token, handler.CancellationToken);
        Assert.Equal(1, handler.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-HANDLER", "typed-response")]
    public async Task ResponseHandler_ReturnsItsExactTypedResponseAsync()
    {
        var handler = new RecordingResponseHandler();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Consumer(() => handler);
        });
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var request = new ResponseRequest(NewId.NextGuid(), "request");

        ResponseMessage response = await mediator.SendRequestAsync(
            request,
            new RequestTimeout(TimeSpan.FromSeconds(10)),
            source.Token);

        Assert.Equal(new ResponseMessage(request.CorrelationId, "response:request"), response);
        Assert.Same(request, handler.Request);
        Assert.True(handler.CancellationToken.CanBeCanceled);
        Assert.Equal(1, handler.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-HANDLER", "null-response-fails-explicitly")]
    public async Task ResponseHandler_RejectsANullResponseBeforeRespondingAsync()
    {
        var handler = new NullResponseHandler();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Consumer(() => handler);
        });

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendRequestAsync(
                new ResponseRequest(NewId.NextGuid(), "null"),
                new RequestTimeout(TimeSpan.FromSeconds(10)),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            $"The mediator request handler '{typeof(NullResponseHandler).FullName}' returned a null response.",
            failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-HANDLER", "null-consume-context")]
    public async Task HandlerAdapters_RejectANullConsumeContextAsync()
    {
        var oneWay = new RecordingOneWayHandler();
        var response = new RecordingResponseHandler();

        ArgumentNullException oneWayFailure = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            oneWay.ConsumeAsync(null!));
        ArgumentNullException responseFailure = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            response.ConsumeAsync(null!));

        Assert.Equal("context", oneWayFailure.ParamName);
        Assert.Equal("context", responseFailure.ParamName);
        Assert.Equal(0, oneWay.InvocationCount);
        Assert.Equal(0, response.InvocationCount);
    }

    private sealed class RecordingOneWayHandler : MediatorRequestHandler<OneWayRequest>
    {
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);
        public OneWayRequest? Request { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        protected override Task HandleAsync(OneWayRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
            Interlocked.Increment(ref _invocationCount);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingResponseHandler : MediatorRequestHandler<ResponseRequest, ResponseMessage>
    {
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);
        public ResponseRequest? Request { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        protected override Task<ResponseMessage> HandleAsync(ResponseRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
            Interlocked.Increment(ref _invocationCount);
            return Task.FromResult(new ResponseMessage(request.CorrelationId, $"response:{request.Value}"));
        }
    }

    private sealed class NullResponseHandler : MediatorRequestHandler<ResponseRequest, ResponseMessage>
    {
        protected override Task<ResponseMessage> HandleAsync(ResponseRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<ResponseMessage>(null!);
    }

    private sealed record OneWayRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    private sealed record ResponseRequest(Guid CorrelationId, string Value) : IRequest<ResponseMessage>, CorrelatedBy<Guid>;
    private sealed record ResponseMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
}
