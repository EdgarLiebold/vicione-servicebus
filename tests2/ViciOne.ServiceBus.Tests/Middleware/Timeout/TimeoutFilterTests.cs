using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Timeout;

public sealed class TimeoutFilterTests
{
    private static readonly DateTimeOffset StartTime =
        new(2031, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "virtual-deadline")]
    public async Task ConfiguredDeadline_CancelsTheActivePipelineStageOnlyWhenContextTimeAdvances(
        bool blockConsumeCompletion)
    {
        TimeSpan timeout = TimeSpan.FromMinutes(3);
        var timeProvider = new ObservableTimeProvider(StartTime);
        ConsumeContext input = CreateContext(CancellationToken.None, timeProvider, Task.CompletedTask);
        CancellationToken observedToken = default;
        var filter = new TimeoutFilter<ConsumeContext, ConsumeContext>(
            (_, token) =>
            {
                observedToken = token;
                Task consumeCompleted = blockConsumeCompletion
                    ? Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, token)
                    : Task.CompletedTask;
                return CreateContext(token, timeProvider, consumeCompleted);
            },
            timeout);
        var pipe = new DelegatePipe<ConsumeContext>(context => blockConsumeCompletion
            ? Task.CompletedTask
            : Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken));

        Task send = filter.Send(input, pipe);

        Assert.False(send.IsCompleted);
        timeProvider.Advance(timeout - TimeSpan.FromTicks(1));
        Assert.False(send.IsCompleted);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        ConsumerCanceledException exception = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            send.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        var cancellation = Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);

        Assert.True(observedToken.IsCancellationRequested);
        Assert.Equal(observedToken, cancellation.CancellationToken);
        Assert.Contains(timeout.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "caller-cancellation-identity")]
    public async Task CallerCancellation_PreservesTheExactCallerTokenAndIsNotReportedAsATimeout(
        bool introduceChildCancellationLayer)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var caller = new CancellationTokenSource();
        ConsumeContext input = CreateContext(caller.Token, timeProvider, Task.CompletedTask);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var filter = CreateFilter(timeProvider);
        var pipe = new DelegatePipe<ConsumeContext>(async context =>
        {
            entered.TrySetResult();
            if (!introduceChildCancellationLayer)
            {
                await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken);
                return;
            }

            using var child = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, child.Token);
        });

        Task send = filter.Send(input, pipe);
        await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        caller.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            send.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));

        Assert.Equal(caller.Token, exception.CancellationToken);
        Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "independent-cancellation")]
    public async Task IndependentCancellation_PropagatesWithoutBeingReclassified()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var independent = new CancellationTokenSource();
        independent.Cancel();
        var expected = new OperationCanceledException("independent", independent.Token);
        ConsumeContext input = CreateContext(CancellationToken.None, timeProvider, Task.CompletedTask);
        var filter = CreateFilter(timeProvider);
        var pipe = new DelegatePipe<ConsumeContext>(_ => Task.FromException(expected));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filter.Send(input, pipe));

        Assert.Same(expected, actual);
        Assert.Equal(independent.Token, actual.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "consume-completion-and-timer-lifetime")]
    public async Task SuccessfulPipeline_WaitsForConsumeCompletionAndDisposesItsDeadline()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var consumeCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsumeContext input = CreateContext(CancellationToken.None, timeProvider, Task.CompletedTask);
        CancellationToken observedToken = default;
        var filter = new TimeoutFilter<ConsumeContext, ConsumeContext>(
            (_, token) =>
            {
                observedToken = token;
                return CreateContext(token, timeProvider, consumeCompleted.Task);
            },
            TimeSpan.FromMinutes(1));

        Task send = filter.Send(input, new DelegatePipe<ConsumeContext>(_ => Task.CompletedTask));

        Assert.False(send.IsCompleted);
        Assert.Equal(1, timeProvider.ActiveTimerCount);
        consumeCompleted.TrySetResult();
        await send.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(0, timeProvider.ActiveTimerCount);
        timeProvider.Advance(TimeSpan.FromDays(1));
        Assert.False(observedToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "explicit-time-provider-override")]
    public async Task ExplicitTimeProvider_OverridesTheProviderAttachedToTheContext()
    {
        TimeSpan timeout = TimeSpan.FromMinutes(1);
        var contextProvider = new ObservableTimeProvider(StartTime);
        var configuredProvider = new ObservableTimeProvider(StartTime);
        ConsumeContext input = CreateContext(CancellationToken.None, contextProvider, Task.CompletedTask);
        var filter = new TimeoutFilter<ConsumeContext, ConsumeContext>(
            (_, token) => CreateContext(token, contextProvider, Task.CompletedTask),
            timeout,
            configuredProvider);
        var pipe = new DelegatePipe<ConsumeContext>(context =>
            Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, context.CancellationToken));

        Task send = filter.Send(input, pipe);
        Assert.Equal(0, contextProvider.ActiveTimerCount);
        Assert.Equal(1, configuredProvider.ActiveTimerCount);
        contextProvider.Advance(TimeSpan.FromDays(1));
        Assert.False(send.IsCompleted);

        configuredProvider.Advance(timeout);
        await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            send.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(0, configuredProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "factory-contract")]
    public async Task ContextFactoryReturningNull_FailsBeforeInvokingTheNextPipe()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        ConsumeContext input = CreateContext(CancellationToken.None, timeProvider, Task.CompletedTask);
        var nextInvoked = false;
        var filter = new TimeoutFilter<ConsumeContext, ConsumeContext>((_, _) => null!, TimeSpan.FromMinutes(1));
        var pipe = new DelegatePipe<ConsumeContext>(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.Send(input, pipe));

        Assert.Equal("The timeout context factory returned null.", exception.Message);
        Assert.False(nextInvoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "null-dependencies")]
    public async Task PublicBoundary_RejectsNullDependenciesWithExactParameterNames()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        ConsumeContext context = CreateContext(CancellationToken.None, timeProvider, Task.CompletedTask);
        var filter = CreateFilter(timeProvider);

        Assert.Equal("contextFactory", Assert.Throws<ArgumentNullException>(() =>
            new TimeoutFilter<ConsumeContext, ConsumeContext>(null!, TimeSpan.FromSeconds(1))).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new TimeoutFilter<ConsumeContext, ConsumeContext>(
                (current, _) => current,
                TimeSpan.FromSeconds(1),
                null!)).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.Send(null!, new DelegatePipe<ConsumeContext>(_ => Task.CompletedTask)))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.Send(context, null!))).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-TIMEOUT-FILTER", "positive-duration")]
    public void Construction_RejectsNonpositiveTimeouts(int ticks)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimeoutFilter<ConsumeContext, ConsumeContext>((context, _) => context, TimeSpan.FromTicks(ticks)));

        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal(TimeSpan.FromTicks(ticks), exception.ActualValue);
    }

    private static TimeoutFilter<ConsumeContext, ConsumeContext> CreateFilter(TimeProvider timeProvider) =>
        new(
            (_, token) => CreateContext(token, timeProvider, Task.CompletedTask),
            TimeSpan.FromMinutes(1));

    private static ConsumeContext CreateContext(
        CancellationToken cancellationToken,
        TimeProvider timeProvider,
        Task consumeCompleted)
    {
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(cancellationToken, timeProvider, consumeCompleted);
        return context;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task Send(TContext context) => callback(context);

        public void Probe(ProbeContext context) => context.CreateScope("delegate");
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private TimeProvider? _timeProvider;
        private Task? _consumeCompleted;

        public void Configure(CancellationToken cancellationToken, TimeProvider timeProvider, Task consumeCompleted)
        {
            _cancellationToken = cancellationToken;
            _timeProvider = timeProvider;
            _consumeCompleted = consumeCompleted;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_CancellationToken")
                return _cancellationToken;
            if (targetMethod.Name == "get_ConsumeCompleted")
                return _consumeCompleted ?? throw new InvalidOperationException("The proxy was not configured.");
            if (targetMethod.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                if (payloadType == typeof(TimeProvider))
                {
                    args![0] = _timeProvider ?? throw new InvalidOperationException("The proxy was not configured.");
                    return true;
                }

                args![0] = null;
                return false;
            }

            throw new NotSupportedException($"Unexpected consume-context member: {targetMethod.Name}");
        }
    }
}
