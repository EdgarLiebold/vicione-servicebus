using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class DynamicRoutingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "all-and-only-compatible-routes")]
    public async Task DynamicRouter_InvokesEveryCompatibleRouteAndDisconnectsItExactlyAsync()
    {
        IDynamicRouter<IRouteContext> router = new DynamicRouter<IRouteContext>(new RouteConverterFactory());
        var aCount = 0;
        var bCount = 0;
        ConnectHandle routeA = router.ConnectPipe(Pipe.Execute<IRouteContext<RouteA>>(_ =>
            Interlocked.Increment(ref aCount)));
        ConnectHandle routeB = router.ConnectPipe(Pipe.Execute<IRouteContext<RouteB>>(_ =>
            Interlocked.Increment(ref bCount)));

        await router.SendAsync(new RoutedContext<RouteA>("a"));
        await router.SendAsync(new RoutedContext<RouteB>("b"));
        await router.SendAsync(new DualRoutedContext("both"));

        Assert.Equal(2, aCount);
        Assert.Equal(2, bCount);

        routeA.Disconnect();
        await router.SendAsync(new RoutedContext<RouteA>("a-after-disconnect"));

        Assert.Equal(2, aCount);
        Assert.Equal(2, bCount);
        routeB.Disconnect();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DISPATCH", "matching-output-and-single-continuation")]
    public async Task UseDispatch_RoutesMatchingContextsAndContinuesTheInputPipeExactlyOnceAsync()
    {
        var trace = new List<string>();
        IPipe<IRouteContext> pipe = Pipe.New<IRouteContext>(configuration =>
        {
            configuration.UseDispatch(new RouteConverterFactory(), dispatch =>
            {
                dispatch.Pipe<IRouteContext<RouteA>>(route =>
                    route.UseExecute(context => trace.Add($"route:{context.Key}")));
            });
            configuration.UseExecute(context => trace.Add($"next:{context.Key}"));
        });

        await pipe.SendAsync(new RoutedContext<RouteA>("a"));
        await pipe.SendAsync(new RoutedContext<RouteB>("b"));

        Assert.Equal(["route:a", "next:a", "next:b"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "sync-output-failure-awaits-started-outputs-and-visits-remaining-routes")]
    public async Task SynchronousConverterFailure_AwaitsStartedOutputAndVisitsRemainingRoutesAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new ExpectedConversionException();
        var filter = new DynamicFilter<IRouteContext>(new ThrowingRouteConverterFactory(expected));
        var firstCompleted = 0;
        var thirdCalls = 0;
        var continuations = 0;
        filter.ConnectPipe(Pipe.ExecuteAwaited<IRouteContext<RouteA>>(async _ =>
        {
            entered.TrySetResult();
            await release.Task;
            Interlocked.Increment(ref firstCompleted);
        }));
        filter.ConnectPipe(Pipe.Empty<IRouteContext<RouteB>>());
        filter.ConnectPipe(Pipe.Execute<IRouteContext<RouteC>>(_ => Interlocked.Increment(ref thirdCalls)));

        Task dispatch = filter.SendAsync(new TripleRoutedContext("all"),
            Pipe.Execute<IRouteContext>(_ => Interlocked.Increment(ref continuations)));
        try
        {
            await entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.False(dispatch.IsCompleted);
            Assert.Equal(1, thirdCalls);
            Assert.Equal(0, continuations);
        }
        finally
        {
            release.TrySetResult();
        }

        ExpectedConversionException actual = await Assert.ThrowsAsync<ExpectedConversionException>(() =>
            dispatch.WaitAsync(timeout, TestContext.Current.CancellationToken));
        Assert.Same(expected, actual);
        Assert.Equal(1, firstCompleted);
        Assert.Equal(1, thirdCalls);
        Assert.Equal(0, continuations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "probe-uses-stable-registration-snapshot")]
    public async Task Probe_UsesStableSnapshotWhenAnOutputRegistersAnotherRouteAsync()
    {
        var filter = new DynamicFilter<IRouteContext>(new RouteConverterFactory());
        var probeCalls = 0;
        var addedRouteCalls = 0;
        filter.ConnectPipe(new ProbeActionPipe<IRouteContext<RouteA>>(() =>
        {
            probeCalls++;
            filter.ConnectPipe(Pipe.Execute<IRouteContext<RouteB>>(_ => addedRouteCalls++));
        }));

        IProbeResult result = filter.GetProbeResult(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(1, probeCalls);

        await filter.SendAsync(new DualRoutedContext("both"), Pipe.Empty<IRouteContext>());
        Assert.Equal(1, addedRouteCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "null-output-task-rejected-for-single-and-multiple-routes")]
    public async Task NullOutputTask_IsRejectedForSingleAndMultipleRoutesAsync(bool multiple)
    {
        var filter = new NullTaskDynamicFilter();
        filter.ConnectPipe(Pipe.Empty<IRouteContext<RouteA>>());
        if (multiple)
            filter.ConnectPipe(Pipe.Empty<IRouteContext<RouteB>>());

        Task dispatch = filter.SendAsync(new DualRoutedContext("both"), Pipe.Empty<IRouteContext>());
        Assert.NotNull(dispatch);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatch);
        Assert.Equal("An output filter returned a null task.", actual.Message);
        Assert.Equal(multiple ? 2 : 1, filter.OutputCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KEYED-DYNAMIC-ROUTING", "type-key-and-disconnection")]
    public async Task KeyedRouter_RequiresBothACompatibleTypeAndTheConnectedKeyAsync()
    {
        IDynamicRouter<IRouteContext, string> router =
            new DynamicRouter<IRouteContext, string>(new RouteConverterFactory(), context => context.Key);
        var routed = 0;
        ConnectHandle east = router.ConnectPipe("east", Pipe.Execute<IRouteContext<RouteA>>(_ =>
            Interlocked.Increment(ref routed)));

        await router.SendAsync(new RoutedContext<RouteA>("east"));
        await router.SendAsync(new RoutedContext<RouteA>("west"));
        await router.SendAsync(new RoutedContext<RouteB>("east"));

        Assert.Equal(1, routed);
        Assert.Throws<DuplicateKeyPipeConfigurationException>(() =>
            router.ConnectPipe("east", Pipe.Empty<IRouteContext<RouteA>>()));
        Assert.Throws<ArgumentNullException>(() =>
            router.ConnectPipe(null!, Pipe.Empty<IRouteContext<RouteA>>()));

        east.Disconnect();
        await router.SendAsync(new RoutedContext<RouteA>("east"));
        Assert.Equal(1, routed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "invalid-collaborator-boundaries")]
    public async Task DynamicRouting_RejectsMissingCollaboratorsAndInvalidConverterResultsAsync()
    {
        Assert.Throws<ArgumentNullException>(() => new DynamicRouter<IRouteContext>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            new DynamicRouter<IRouteContext, string>(new RouteConverterFactory(), null!));
        Assert.Throws<ArgumentNullException>(() =>
            Pipe.New<IRouteContext>(configuration => configuration.UseDispatch(null!)));

        IDynamicRouter<IRouteContext> missingConverter =
            new DynamicRouter<IRouteContext>(new NullConverterFactory());
        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() =>
            missingConverter.ConnectPipe(Pipe.Empty<IRouteContext<RouteA>>()));

        IDynamicRouter<IRouteContext> invalidResult =
            new DynamicRouter<IRouteContext>(new NullResultConverterFactory());
        invalidResult.ConnectPipe(Pipe.Empty<IRouteContext<RouteA>>());
        InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invalidResult.SendAsync(new RoutedContext<RouteA>("a")));

        IDynamicRouter<IRouteContext, string> nullKey =
            new DynamicRouter<IRouteContext, string>(new RouteConverterFactory(), _ => null!);
        nullKey.ConnectPipe("configured", Pipe.Empty<IRouteContext<RouteA>>());
        InvalidOperationException invalidKey = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullKey.SendAsync(new RoutedContext<RouteA>("ignored")));
        IDynamicRouter<IRouteContext> incompatibleType =
            new DynamicRouter<IRouteContext>(new RouteConverterFactory());
        ArgumentException incompatible = Assert.Throws<ArgumentException>(() =>
            incompatibleType.ConnectPipe(Pipe.Empty<UnrelatedContext>()));

        Assert.Contains(nameof(IRouteContext<RouteA>), missing.Message, StringComparison.Ordinal);
        Assert.Contains("success with a null", invalid.Message, StringComparison.Ordinal);
        Assert.Equal("The key accessor returned null.", invalidKey.Message);
        Assert.Contains("must implement", incompatible.Message, StringComparison.Ordinal);
    }

    private interface IRouteContext : PipeContext
    {
        string Key { get; }
    }

    private interface IRouteContext<TMarker> : IRouteContext
        where TMarker : class;

    private sealed class RoutedContext<TMarker>(string key) : BasePipeContext, IRouteContext<TMarker>
        where TMarker : class
    {
        public string Key { get; } = key;
    }

    private sealed class DualRoutedContext(string key) : BasePipeContext, IRouteContext<RouteA>, IRouteContext<RouteB>
    {
        public string Key { get; } = key;
    }

    private sealed class TripleRoutedContext(string key) : BasePipeContext,
        IRouteContext<RouteA>, IRouteContext<RouteB>, IRouteContext<RouteC>
    {
        public string Key { get; } = key;
    }

    private sealed class UnrelatedContext : BasePipeContext;

    private sealed class RouteConverterFactory : IPipeContextConverterFactory<IRouteContext>
    {
        public IPipeContextConverter<IRouteContext, TOutput> GetConverter<TOutput>()
            where TOutput : class, PipeContext => new CastConverter<TOutput>();
    }

    private sealed class CastConverter<TOutput> : IPipeContextConverter<IRouteContext, TOutput>
        where TOutput : class, PipeContext
    {
        public bool TryConvert(IRouteContext input, out TOutput output)
        {
            if (input is TOutput converted)
            {
                output = converted;
                return true;
            }

            output = null!;
            return false;
        }
    }

    private sealed class ThrowingRouteConverterFactory(ExpectedConversionException failure) :
        IPipeContextConverterFactory<IRouteContext>
    {
        public IPipeContextConverter<IRouteContext, TOutput> GetConverter<TOutput>()
            where TOutput : class, PipeContext => typeof(TOutput) == typeof(IRouteContext<RouteB>)
                ? new ThrowingRouteConverter<TOutput>(failure)
                : new CastConverter<TOutput>();
    }

    private sealed class ThrowingRouteConverter<TOutput>(ExpectedConversionException failure) :
        IPipeContextConverter<IRouteContext, TOutput>
        where TOutput : class, PipeContext
    {
        public bool TryConvert(IRouteContext input, out TOutput output) => throw failure;
    }

    private sealed class ProbeActionPipe<TContext>(Action onProbe) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
            onProbe();
            context.CreateScope("probe-action");
        }

        public Task SendAsync(TContext context) => Task.CompletedTask;
    }

    private sealed class NullTaskDynamicFilter() : DynamicFilter<IRouteContext>(new RouteConverterFactory())
    {
        public int OutputCalls { get; private set; }

        protected override IOutputFilter CreateOutputPipe<TOutput>() => new NullTaskOutputFilter<TOutput>(this);

        private sealed class NullTaskOutputFilter<TOutput>(NullTaskDynamicFilter owner) :
            IOutputFilter, IPipeConnector<TOutput>
            where TOutput : class, PipeContext
        {
            private readonly ViciOne.ServiceBus.Util.Connectable<IPipe<TOutput>> _connections = new();

            public TResult As<TResult>() where TResult : class => this as TResult
                ?? throw new InvalidOperationException("The requested connector is unavailable.");

            public ConnectHandle ConnectPipe(IPipe<TOutput> pipe) => _connections.Connect(pipe);

            public ConnectHandle ConnectObserver<TObserved>(IFilterObserver<TObserved> observer)
                where TObserved : class, PipeContext => throw new NotSupportedException();

            public ConnectHandle ConnectObserver(IFilterObserver observer) => throw new NotSupportedException();

            public void Probe(ProbeContext context)
            {
            }

            public Task SendAsync(IRouteContext context, IPipe<IRouteContext> next)
            {
                owner.OutputCalls++;
                return null!;
            }
        }
    }

    private sealed class NullConverterFactory : IPipeContextConverterFactory<IRouteContext>
    {
        public IPipeContextConverter<IRouteContext, TOutput> GetConverter<TOutput>()
            where TOutput : class, PipeContext => null!;
    }

    private sealed class NullResultConverterFactory : IPipeContextConverterFactory<IRouteContext>
    {
        public IPipeContextConverter<IRouteContext, TOutput> GetConverter<TOutput>()
            where TOutput : class, PipeContext => new NullResultConverter<TOutput>();
    }

    private sealed class NullResultConverter<TOutput> : IPipeContextConverter<IRouteContext, TOutput>
        where TOutput : class, PipeContext
    {
        public bool TryConvert(IRouteContext input, out TOutput output)
        {
            output = null!;
            return true;
        }
    }

    private sealed class RouteA;

    private sealed class RouteB;

    private sealed class RouteC;

    private sealed class ExpectedConversionException : Exception;
}
