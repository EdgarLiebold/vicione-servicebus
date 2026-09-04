using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RescueFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "matching-exception-and-custom-context")]
    public async Task Rescue_PassesTheExactHandledFailureToTheCustomRescueContextAsync()
    {
        var expected = new HandledException("handled");
        RescueContext? rescued = null;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception),
                rescue =>
                {
                    rescue.Handle<HandledException>();
                    rescue.UseExecute(context => rescued = context);
                });
            configuration.UseExecute(_ => throw expected);
        });

        var source = new TestPipeContext();
        await pipe.SendAsync(source);

        Assert.NotNull(rescued);
        Assert.Same(source, rescued.Source);
        Assert.Same(expected, rescued.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "ignored-exception-rethrown")]
    public async Task Rescue_RethrowsTheExactExceptionExcludedByTheFilterAsync()
    {
        var expected = new HandledException("ignored");
        var rescueCount = 0;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception),
                rescue =>
                {
                    rescue.Ignore<HandledException>();
                    rescue.UseExecute(_ => Interlocked.Increment(ref rescueCount));
                });
            configuration.UseExecute(_ => throw expected);
        });

        HandledException actual = await Assert.ThrowsAsync<HandledException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Same(expected, actual);
        Assert.Equal(0, rescueCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "aggregate-match-preserves-full-failure")]
    public async Task Rescue_MatchesAnAggregateByItsBaseFailureButPreservesTheFullAggregateForDiagnosticsAsync()
    {
        var root = new HandledException("root");
        var expected = new AggregateException("aggregate", root);
        Exception? rescuedFailure = null;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception),
                rescue =>
                {
                    rescue.Handle<HandledException>();
                    rescue.UseExecute(context => rescuedFailure = context.Exception);
                });
            configuration.UseExecute(_ => throw expected);
        });

        await pipe.SendAsync(new TestPipeContext());

        Assert.Same(expected, rescuedFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "rescue-pipe-failure-propagated")]
    public async Task Rescue_PropagatesTheExactFailureRaisedByTheRescuePipeAsync()
    {
        var original = new HandledException("original");
        var rescueFailure = new RescueFailedException("rescue failed");
        IPipe<ITestPipeContext> failingRescue = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception),
                rescue =>
                {
                    rescue.Handle<HandledException>();
                    rescue.UseExecute(_ => throw rescueFailure);
                });
            configuration.UseExecute(_ => throw original);
        });
        RescueFailedException actual = await Assert.ThrowsAsync<RescueFailedException>(() =>
            failingRescue.SendAsync(new TestPipeContext()));

        Assert.Same(rescueFailure, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "null-factory-result-rejected")]
    public async Task Rescue_RejectsANullFactoryResultAsync()
    {
        var original = new HandledException("original");

        IPipe<ITestPipeContext> nullFactory = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (_, _) => null!, rescue => rescue.Handle<HandledException>());
            configuration.UseExecute(_ => throw original);
        });
        InvalidOperationException invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullFactory.SendAsync(new TestPipeContext()));

        Assert.Equal("The rescue context factory returned null.", invalid.Message);
    }

    private interface ITestPipeContext : PipeContext;

    private sealed class TestPipeContext : BasePipeContext, ITestPipeContext;

    private sealed class RescueContext(ITestPipeContext source, Exception exception) :
        ProxyPipeContext(source), ITestPipeContext
    {
        public ITestPipeContext Source { get; } = source;

        public Exception Exception { get; } = exception;
    }

    private sealed class HandledException(string message) : Exception(message);

    private sealed class RescueFailedException(string message) : Exception(message);
}
