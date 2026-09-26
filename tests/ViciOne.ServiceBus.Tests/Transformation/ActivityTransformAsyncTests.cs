using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transformation;

public sealed class ActivityTransformAsyncTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TRANSFORM", "pending-initialization-preserves-or-replaces-data-and-awaits-next")]
    public Task PendingInitialization_PreservesContextAndAwaitsTheDownstreamOutcomeAsync(bool compensate, bool replace) =>
        compensate
            ? CheckSuccessAsync<CompensateContext<Data>>(replace, context => context.Log)
            : CheckSuccessAsync<ExecuteContext<Data>>(replace, context => context.Arguments);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TRANSFORM", "pending-initialization-failure-preserves-cause-and-suppresses-next")]
    public Task PendingInitializationFailure_PreservesTheCauseWithoutInvokingTheNextStageAsync(bool compensate) =>
        compensate ? CheckFailureAsync<CompensateContext<Data>>() : CheckFailureAsync<ExecuteContext<Data>>();

    private static async Task CheckSuccessAsync<TContext>(bool replace, Func<TContext, Data> getData)
        where TContext : class, ActivityContext
    {
        using var owner = new CancellationTokenSource();
        var fixture = new Fixture(owner.Token);
        TContext original = fixture.CreateContext<TContext>();
        var next = new RecordingPipe<TContext>();
        var filter = (IFilter<TContext>)(object)new TransformFilter<Data>(fixture.Initializer);

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
        Assert.Equal(fixture.TrackingNumber, forwarded.TrackingNumber);
        Assert.Equal(owner.Token, forwarded.CancellationToken);
        if (replace)
            Assert.NotSame(original, forwarded);
        else
            Assert.Same(original, forwarded);
        Assert.False(operation.IsCompleted);
        var expected = new InvalidOperationException("downstream rejected the transformed activity");
        next.Completion.SetException(expected);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => operation.WaitAsync(Timeout(), TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, next.Calls);
        Assert.Equal("original", fixture.Original.Value);
    }

    private static async Task CheckFailureAsync<TContext>() where TContext : class, ActivityContext
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

        public TContext CreateContext<TContext>() where TContext : class => Strict<TContext>((method, _) => method.Name switch
        {
            "get_Arguments" or "get_Log" => Original,
            "get_TrackingNumber" => TrackingNumber,
            "get_CancellationToken" => _token,
            "get_ReceiveContext" => _receive,
            "get_SerializerContext" => _serializer,
            _ => throw new InvalidOperationException(method.Name),
        });
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
