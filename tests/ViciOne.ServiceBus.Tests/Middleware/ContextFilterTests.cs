using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ContextFilterTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    [RequirementCoverage("REQ-VSB-CONTEXT-FILTER", "matching-context-only")]
    public async Task Filter_ForwardsOnlyAnAcceptedContextAsync(bool accepted, int expectedInvocations)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var context = new FilterContext("expected");
        var observed = new List<FilterContext>();
        var filter = new ContextFilter<FilterContext>(_ => Task.FromResult(accepted));
        IPipe<FilterContext> next = Pipe.Execute<FilterContext>(observed.Add);

        await filter.SendAsync(context, next).WaitAsync(timeout, cancellationToken);

        Assert.Equal(expectedInvocations, observed.Count);
        if (accepted)
            Assert.Same(context, Assert.Single(observed));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    [RequirementCoverage("REQ-VSB-CONTEXT-FILTER", "asynchronous-decision-is-awaited")]
    public async Task Filter_WaitsForTheAsynchronousDecisionBeforeApplyingItAsync(
        bool accepted,
        int expectedInvocations)
    {
        var decision = NewSignal<bool>();
        var context = new FilterContext("asynchronous");
        var invocations = 0;
        var filter = new ContextFilter<FilterContext>(_ => decision.Task);
        IPipe<FilterContext> next = Pipe.Execute<FilterContext>(observed =>
        {
            Assert.Same(context, observed);
            Interlocked.Increment(ref invocations);
        });

        Task send = filter.SendAsync(context, next);
        Assert.False(send.IsCompleted);
        Assert.Equal(0, Volatile.Read(ref invocations));

        decision.SetResult(accepted);
        await send.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(expectedInvocations, Volatile.Read(ref invocations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-FILTER", "invalid-public-boundaries")]
    public async Task Filter_RejectsNullCollaboratorsAndANullDecisionTaskAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            new ContextFilter<FilterContext>(null!)).ParamName);

        var valid = new ContextFilter<FilterContext>(_ => Task.FromResult(true));
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            valid.SendAsync(null!, Pipe.Empty<FilterContext>()).WaitAsync(timeout, cancellationToken))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            valid.SendAsync(new FilterContext("value"), null!).WaitAsync(timeout, cancellationToken))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => valid.Probe(null!)).ParamName);

        var nullTask = new ContextFilter<FilterContext>(_ => null!);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullTask.SendAsync(new FilterContext("value"), Pipe.Empty<FilterContext>())
                .WaitAsync(timeout, cancellationToken));
        Assert.Equal("The context filter returned a null decision task.", actual.Message);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class FilterContext(string value) : BasePipeContext
    {
        public string Value { get; } = value;
    }
}
