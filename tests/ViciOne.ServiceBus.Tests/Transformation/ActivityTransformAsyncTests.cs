using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transformation;

public sealed class ActivityTransformAsyncTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SEND-TRANSFORM", "pending-initialization-preserves-envelope-and-awaits-send-outcome")]
    public Task PendingSendInitialization_PreservesEnvelopeAndAwaitsTheSendOutcomeAsync(bool replace, bool downstreamFails) =>
        CheckSuccessAsync<SendContext<Data>>(replace, downstreamFails, context => context.Message);

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TRANSFORM", "pending-initialization-failure-never-reaches-send")]
    public Task PendingSendInitializationFailure_NeverReachesTheSendPipeAsync() => CheckFailureAsync<SendContext<Data>>();

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TRANSFORM", "pending-initialization-preserves-or-replaces-data-and-awaits-next")]
    public Task PendingInitialization_PreservesContextAndAwaitsTheDownstreamOutcomeAsync(bool compensate, bool replace, bool downstreamFails) =>
        compensate
            ? CheckSuccessAsync<CompensateContext<Data>>(replace, downstreamFails, context => context.Log)
            : CheckSuccessAsync<ExecuteContext<Data>>(replace, downstreamFails, context => context.Arguments);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONSUME-TRANSFORM", "pending-initialization-preserves-message-envelope-and-downstream-outcome")]
    public Task PendingConsumeInitialization_PreservesMessageAndEnvelopeUntilDownstreamCompletesAsync(bool replace, bool downstreamFails) =>
        CheckSuccessAsync<ConsumeContext<Data>>(replace, downstreamFails, context => context.Message);

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TRANSFORM", "pending-initialization-failure-never-reaches-consumer")]
    public Task PendingConsumeInitializationFailure_NeverReachesTheConsumerAsync() => CheckFailureAsync<ConsumeContext<Data>>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TRANSFORM", "pending-initialization-failure-preserves-cause-and-suppresses-next")]
    public Task PendingInitializationFailure_PreservesTheCauseWithoutInvokingTheNextStageAsync(bool compensate) =>
        compensate ? CheckFailureAsync<CompensateContext<Data>>() : CheckFailureAsync<ExecuteContext<Data>>();

    private static async Task CheckSuccessAsync<TContext>(bool replace, bool downstreamFails, Func<TContext, Data> getData)
        where TContext : class, PipeContext
    {
        using var owner = new CancellationTokenSource();
        var fixture = new Fixture(owner.Token);
        TContext original = fixture.CreateContext<TContext>();
        var next = new RecordingPipe<TContext>();
        var filter = (IFilter<TContext>)(object)new TransformFilter<Data>(fixture.Initializer);

        var outbound = original as SendContext<Data>;
        Guid? messageId = outbound?.MessageId;
        Guid? requestId = outbound?.RequestId;
        Uri? destination = outbound?.DestinationAddress;

        Task operation = filter.SendAsync(original, next);

        Assert.False(operation.IsCompleted);
        Assert.Equal(0, next.Calls);
        Assert.Same(fixture.Original, fixture.Input);
        Assert.Same(fixture.Seed, fixture.InitializationContext);
        Assert.Equal(owner.Token, fixture.InheritedContext!.CancellationToken);
        Data result = replace ? new Data("transformed") : fixture.Original;
        fixture.Completion.SetResult(Strict<InitializeContext<Data>>((method, _) => method.Name == "get_Message"
            ? result : throw new InvalidOperationException(method.Name)));
        TContext forwarded = await next.Entered.Task.WaitAsync(Timeout(), TestContext.Current.CancellationToken);

        Assert.Same(result, getData(forwarded));
        if (forwarded is ActivityContext activity)
            Assert.Equal(fixture.TrackingNumber, activity.TrackingNumber);
        else if (forwarded is SendContext<Data> send)
        {
            var originalSend = (SendContext<Data>)(object)original;
            Assert.Equal(fixture.TrackingNumber, send.CorrelationId);
            Assert.Equal(messageId, send.MessageId);
            Assert.Equal(destination, send.DestinationAddress);
            Assert.Equal(requestId, send.RequestId);
            Assert.Equal(messageId, originalSend.MessageId);
            Assert.Equal(destination, originalSend.DestinationAddress);
            Assert.Equal(requestId, originalSend.RequestId);
            Assert.Same(originalSend.Headers, send.Headers);
            Assert.Equal("trace-value", send.Headers.Get<string>("transform-trace"));
            Assert.Same(fixture.Original, originalSend.Message);
        }
        else
            Assert.Equal(fixture.TrackingNumber, ((ConsumeContext<Data>)(object)forwarded).CorrelationId);
        Assert.Equal(owner.Token, forwarded.CancellationToken);
        if (replace)
            Assert.NotSame(original, forwarded);
        else
            Assert.Same(original, forwarded);
        Assert.False(operation.IsCompleted);
        if (downstreamFails)
        {
            var expected = new InvalidOperationException("downstream rejected the transformed message");
            next.Completion.SetException(expected);
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => operation.WaitAsync(Timeout(), TestContext.Current.CancellationToken));
            Assert.Same(expected, actual);
        }
        else
        {
            next.Completion.SetResult();
            await operation.WaitAsync(Timeout(), TestContext.Current.CancellationToken);
            Assert.True(operation.IsCompletedSuccessfully);
        }
        Assert.Equal(1, next.Calls);
        Assert.Equal("original", fixture.Original.Value);
    }

    private static async Task CheckFailureAsync<TContext>() where TContext : class, PipeContext
    {
        using var owner = new CancellationTokenSource();
        var fixture = new Fixture(owner.Token);
        TContext original = fixture.CreateContext<TContext>();
        var next = new RecordingPipe<TContext>();
        var filter = (IFilter<TContext>)(object)new TransformFilter<Data>(fixture.Initializer);
        var expected = new InvalidOperationException("initializer failed after asynchronous work");

        Task operation = filter.SendAsync(original, next);
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, next.Calls);
        fixture.Completion.SetException(expected);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => operation.WaitAsync(Timeout(), TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(0, next.Calls);
        Assert.False(next.Entered.Task.IsCompleted);
        Assert.Same(fixture.Original, fixture.Input);
    }

    private static TimeSpan Timeout() => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    private sealed class Fixture
    {
        private readonly CancellationToken _token;
        private readonly ReceiveContext _receive;
        private readonly SerializerContext _serializer = Strict<SerializerContext>((method, _) => throw new InvalidOperationException(method.Name));

        public Fixture(CancellationToken token)
        {
            _token = token;
            IPublishEndpointProvider provider = Strict<IPublishEndpointProvider>((method, _) => throw new InvalidOperationException(method.Name));
            _receive = Strict<ReceiveContext>((method, _) => method.Name == "get_PublishEndpointProvider"
                ? provider : throw new InvalidOperationException(method.Name));
            Seed = Strict<InitializeContext<Data>>((method, _) => method.Name == "get_Message"
                ? Original : throw new InvalidOperationException(method.Name));
            Initializer = Strict<IMessageInitializer<Data>>((method, args) =>
            {
                if (method.Name == "Create" && args![0] is PipeContext context)
                {
                    InheritedContext = context;
                    return Seed;
                }
                if (method.Name == "InitializeAsync" && args!.Length == 3)
                {
                    InitializationContext = args[0];
                    Input = args[1];
                    return Completion.Task;
                }
                throw new InvalidOperationException(method.Name);
            });
        }

        public Data Original { get; } = new("original");
        public Guid TrackingNumber { get; } = NewId.NextGuid();
        public InitializeContext<Data> Seed { get; }
        public IMessageInitializer<Data> Initializer { get; }
        public PipeContext? InheritedContext { get; private set; }
        public object? InitializationContext { get; private set; }
        public object? Input { get; private set; }
        public TaskCompletionSource<InitializeContext<Data>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TContext CreateContext<TContext>() where TContext : class
        {
            if (typeof(TContext) == typeof(SendContext<Data>))
            {
                var send = new MessageSendContext<Data>(Original, _token)
                {
                    CorrelationId = TrackingNumber,
                    RequestId = NewId.NextGuid(),
                    DestinationAddress = new Uri("loopback://localhost/transformed-send"),
                };
                send.Headers.Set("transform-trace", "trace-value");
                return (TContext)(object)send;
            }

            if (typeof(TContext) == typeof(ConsumeContext<Data>))
                return (TContext)(object)new MessageConsumeContext<Data>(CreateContext<ConsumeContext>(), Original);

            return Strict<TContext>((method, _) => method.Name switch
            {
                "get_Arguments" or "get_Log" => Original,
                "get_TrackingNumber" or "get_CorrelationId" => TrackingNumber,
                "get_CancellationToken" => _token,
                "get_ReceiveContext" => _receive,
                "get_SerializerContext" => _serializer,
                _ => throw new InvalidOperationException(method.Name),
            });
        }
    }

    private sealed class RecordingPipe<TContext> : IPipe<TContext> where TContext : class, PipeContext
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource<TContext> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(TContext context)
        {
            Interlocked.Increment(ref _calls);
            Entered.TrySetResult(context);
            return Completion.Task;
        }

        public void Probe(ProbeContext context) => throw new NotSupportedException();
    }

    public sealed record Data(string Value);

    private static T Strict<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        T result = DispatchProxy.Create<T, StrictProxy>();
        ((StrictProxy)(object)result).Handler = invoke;
        return result;
    }

    private class StrictProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
