using System.Reflection;
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
    [RequirementCoverage("REQ-VSB-KEYED-DYNAMIC-ROUTING", "stale-handle-cannot-remove-replacement")]
    public async Task KeyedRouter_StaleHandleCannotDisconnectAReplacementAsync()
    {
        IDynamicRouter<IRouteContext, string> router =
            new DynamicRouter<IRouteContext, string>(new RouteConverterFactory(), context => context.Key);
        var firstCalls = 0;
        var replacementCalls = 0;
        ConnectHandle first = router.ConnectPipe("east", Pipe.Execute<IRouteContext<RouteA>>(_ => firstCalls++));
        await router.SendAsync(new RoutedContext<RouteA>("east"));

        first.Disconnect();
        ConnectHandle replacement = router.ConnectPipe("east",
            Pipe.Execute<IRouteContext<RouteA>>(_ => replacementCalls++));
        first.Disconnect();

        await router.SendAsync(new RoutedContext<RouteA>("east"));
        Assert.Equal(1, firstCalls);
        Assert.Equal(1, replacementCalls);

        replacement.Disconnect();
        await router.SendAsync(new RoutedContext<RouteA>("east"));
        Assert.Equal(1, replacementCalls);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-KEYED-DYNAMIC-ROUTING", "request-id-stale-disposal-preserves-replacement-registration")]
    public async Task RequestIdRoute_StaleDisposalCannotDisconnectItsReplacementAsync(bool disconnectFirst)
    {
        var requestId = Guid.Parse("8592b655-b274-4c5e-a161-2aab13093561");
        ConsumeContext<RouteA> context = DispatchProxy.Create<ConsumeContext<RouteA>, RequestIdConsumeContextProxy>();
        ((RequestIdConsumeContextProxy)(object)context).RequestId = requestId;
        var filter = new RequestIdFilter<RouteA>();
        var firstCalls = 0;
        var replacementCalls = 0;
        var nextCalls = 0;
        ConsumeContext<RouteA>? received = null;
        IPipe<ConsumeContext<RouteA>> next = Pipe.ExecuteAwaited<ConsumeContext<RouteA>>(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        ConnectHandle first = filter.ConnectPipe(requestId, Pipe.ExecuteAwaited<ConsumeContext<RouteA>>(actual =>
        {
            firstCalls++;
            received = actual;
            return Task.CompletedTask;
        }));
        ConnectHandle? replacement = null;
        try
        {
            await filter.SendAsync(context, next);
            Assert.Equal(1, firstCalls);
            Assert.Same(context, received);
            Assert.Equal(1, nextCalls);
            if (disconnectFirst)
                first.Disconnect();
            else
                first.Dispose();
            await filter.SendAsync(context, next);
            Assert.Equal(1, firstCalls);
            Assert.Equal(2, nextCalls);
            replacement = filter.ConnectPipe(requestId, Pipe.ExecuteAwaited<ConsumeContext<RouteA>>(actual =>
            {
                replacementCalls++;
                received = actual;
                return Task.CompletedTask;
            }));
            await filter.SendAsync(context, next);
            Assert.Equal(1, replacementCalls);
            Assert.Same(context, received);
            Assert.Equal(3, nextCalls);
            first.Dispose();
            await filter.SendAsync(context, next);
            Assert.Equal(2, replacementCalls);
            Assert.Equal(1, firstCalls);
            Assert.Equal(4, nextCalls);
            replacement.Disconnect();
            await filter.SendAsync(context, next);
            Assert.Equal(2, replacementCalls);
            Assert.Equal(5, nextCalls);
        }
        finally
        {
            replacement?.Dispose();
            first.Dispose();
        }
    }

    public class RequestIdConsumeContextProxy : DispatchProxy
    {
        public Guid? RequestId { get; set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_RequestId"
                ? RequestId
                : throw new NotSupportedException($"Unexpected request-routing context member: {targetMethod?.Name}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "fork-secondary-failure-joins-started-primary-pipe-task")]
    public async Task Fork_SecondaryFailureKeepsTheStartedSiblingOwnedAsync(bool synchronousFailure)
    {
        var context = new RoutedContext<RouteA>("fork-owned");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var primary = new IOException("unique-fork-secondary-provider-failure");
        var firstCalls = 0;
        var secondCalls = 0;
        IRouteContext? firstContext = null;
        IRouteContext? secondContext = null;
        Task? secondTask = null;
        IPipe<IRouteContext> first = new TaskOutcomeRoutePipe(actual =>
        {
            firstCalls++;
            firstContext = actual;
            return gate.Task;
        });
        IPipe<IRouteContext> next = new TaskOutcomeRoutePipe(actual =>
        {
            secondCalls++;
            secondContext = actual;
            if (synchronousFailure)
                throw primary;
            secondTask = Task.FromException(primary);
            return secondTask;
        });
        IFilter<IRouteContext> filter = new ForkFilter<IRouteContext>(first);
        Task? operation = null;
        try
        {
            Exception? admissionFailure = Record.Exception(() => { operation = filter.SendAsync(context, next); });
            Assert.Equal(1, firstCalls);
            Assert.Equal(1, secondCalls);
            Assert.Same(context, firstContext);
            Assert.Same(context, secondContext);
            Assert.False(gate.Task.IsCompleted);
            Assert.Null(admissionFailure);
            Assert.NotNull(operation);
            Assert.False(operation.IsCompleted);
            gate.SetResult();
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Same(primary, observed);
            Assert.True(operation.IsFaulted);
            Assert.True(gate.Task.IsCompletedSuccessfully);
        }
        finally
        {
            gate.TrySetResult();
            await JoinForkTaskAsync(gate.Task);
            if (secondTask is not null)
                await JoinForkTaskAsync(secondTask);
            if (operation is not null)
                await JoinForkTaskAsync(operation);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "fork-null-task-joins-started-sibling")]
    public async Task Fork_NullTaskJoinsTheActualStartedSiblingBeforeFaultingAsync(bool firstIsNull)
    {
        var context = new RoutedContext<RouteA>("fork-null-owned");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<IRouteContext>();
        IPipe<IRouteContext> first = new TaskOutcomeRoutePipe(actual =>
        {
            calls.Add(actual);
            return firstIsNull ? null! : gate.Task;
        });
        IPipe<IRouteContext> next = new TaskOutcomeRoutePipe(actual =>
        {
            calls.Add(actual);
            return firstIsNull ? gate.Task : null!;
        });
        IFilter<IRouteContext> filter = new ForkFilter<IRouteContext>(first);
        Task? operation = null;
        try
        {
            Exception? admissionFailure = Record.Exception(() => { operation = filter.SendAsync(context, next); });
            Assert.Equal(2, calls.Count);
            Assert.All(calls, actual => Assert.Same(context, actual));
            Assert.False(gate.Task.IsCompleted);
            Assert.Null(admissionFailure);
            Assert.NotNull(operation);
            Assert.False(operation.IsCompleted);
            gate.SetResult();
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.IsType<ArgumentException>(observed);
            Assert.True(operation.IsFaulted);
            Assert.True(gate.Task.IsCompletedSuccessfully);
            Assert.Same(observed, Assert.Single(operation.Exception!.InnerExceptions));
        }
        finally
        {
            gate.TrySetResult();
            await JoinForkTaskAsync(gate.Task);
            if (operation is not null)
                await JoinForkTaskAsync(operation);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "fork-first-synchronous-failure-stops-sibling-admission")]
    public void Fork_FirstSynchronousFailurePreservesAdmissionAndStopsTheSibling()
    {
        var context = new RoutedContext<RouteA>("fork-first-failure");
        var primary = new IOException("unique-fork-first-synchronous-failure");
        var firstCalls = 0;
        var secondCalls = 0;
        IRouteContext? firstContext = null;
        IPipe<IRouteContext> first = new TaskOutcomeRoutePipe(actual =>
        {
            firstCalls++;
            firstContext = actual;
            throw primary;
        });
        IPipe<IRouteContext> next = new TaskOutcomeRoutePipe(_ =>
        {
            secondCalls++;
            return Task.CompletedTask;
        });
        IFilter<IRouteContext> filter = new ForkFilter<IRouteContext>(first);
        Task? operation = null;
        Exception? observed = Record.Exception(() => { operation = filter.SendAsync(context, next); });
        Assert.Same(primary, observed);
        Assert.Equal(1, firstCalls);
        Assert.Same(context, firstContext);
        Assert.Equal(0, secondCalls);
        Assert.Null(operation);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "fork-canceled-sibling-waits-owned-pipe")]
    public async Task Fork_CanceledSiblingWaitsForTheStartedPipeAndRetainsCancellationAsync()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var context = new RoutedContext<RouteA>("fork-canceled-owned");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<IRouteContext>();
        Task? canceledTask = null;
        IPipe<IRouteContext> first = new TaskOutcomeRoutePipe(actual =>
        {
            calls.Add(actual);
            return gate.Task;
        });
        IPipe<IRouteContext> next = new TaskOutcomeRoutePipe(actual =>
        {
            calls.Add(actual);
            cancellation.Cancel();
            canceledTask = Task.FromCanceled(cancellation.Token);
            return canceledTask;
        });
        IFilter<IRouteContext> filter = new ForkFilter<IRouteContext>(first);
        Task? operation = null;
        try
        {
            Exception? admissionFailure = Record.Exception(() => { operation = filter.SendAsync(context, next); });
            Assert.Equal(2, calls.Count);
            Assert.All(calls, actual => Assert.Same(context, actual));
            Assert.False(gate.Task.IsCompleted);
            Assert.NotNull(canceledTask);
            Assert.True(canceledTask.IsCanceled);
            Assert.Null(admissionFailure);
            Assert.NotNull(operation);
            Assert.False(operation.IsCompleted);
            gate.SetResult();
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            var canceled = Assert.IsAssignableFrom<OperationCanceledException>(observed);
            Assert.Equal(cancellation.Token, canceled.CancellationToken);
            Assert.True(operation.IsCanceled);
            Assert.False(operation.IsFaulted);
            Assert.True(gate.Task.IsCompletedSuccessfully);
        }
        finally
        {
            gate.TrySetResult();
            await JoinForkTaskAsync(gate.Task);
            if (canceledTask is not null)
                await JoinForkTaskAsync(canceledTask);
            if (operation is not null)
                await JoinForkTaskAsync(operation);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-ROUTING", "fork-combined-failures-preserve-actual-causes")]
    public async Task Fork_FaultedPipeAndSynchronousSiblingFailureRetainBothActualCausesAsync()
    {
        var context = new RoutedContext<RouteA>("fork-dual-failure");
        var primary = new IOException("unique-fork-first-task-failure");
        var secondary = new ApplicationException("unique-fork-second-synchronous-failure");
        Task? firstTask = null;
        var calls = new List<IRouteContext>();
        IPipe<IRouteContext> first = new TaskOutcomeRoutePipe(actual =>
        {
            calls.Add(actual);
            firstTask = Task.FromException(primary);
            return firstTask;
        });
        IPipe<IRouteContext> next = new TaskOutcomeRoutePipe(actual =>
        {
            calls.Add(actual);
            throw secondary;
        });
        IFilter<IRouteContext> filter = new ForkFilter<IRouteContext>(first);
        Task? operation = null;
        try
        {
            Exception? admissionFailure = Record.Exception(() => { operation = filter.SendAsync(context, next); });
            Assert.Equal(2, calls.Count);
            Assert.All(calls, actual => Assert.Same(context, actual));
            Assert.NotNull(firstTask);
            Assert.True(firstTask.IsFaulted);
            Assert.Null(admissionFailure);
            Assert.NotNull(operation);
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.True(operation.IsFaulted);
            var causes = operation.Exception!.Flatten().InnerExceptions;
            Assert.Equal(2, causes.Count);
            Assert.Single(causes, cause => ReferenceEquals(primary, cause));
            Assert.Single(causes, cause => ReferenceEquals(secondary, cause));
            Assert.Contains(causes, cause => ReferenceEquals(observed, cause));
        }
        finally
        {
            if (firstTask is not null)
                await JoinForkTaskAsync(firstTask);
            if (operation is not null)
                await JoinForkTaskAsync(operation);
        }
    }

    private static async Task JoinForkTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
            // Observe each actual started provider/public task even after a finite assertion failure.
        }
    }

    private sealed class TaskOutcomeRoutePipe(Func<IRouteContext, Task> send) : IPipe<IRouteContext>
    {
        public Task SendAsync(IRouteContext context) => send(context);
        public void Probe(ProbeContext context) => context.CreateScope("fork-outcome-control");
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
