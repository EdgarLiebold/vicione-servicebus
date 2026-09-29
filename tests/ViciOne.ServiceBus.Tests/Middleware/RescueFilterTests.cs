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
    public async Task Rescue_MatchesAnAggregateInnerButPreservesTheFullAggregateForDiagnosticsAsync()
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
    [RequirementCoverage("REQ-VSB-RESCUE", "nested-aggregate-leaf-selects-rescue-with-original-failure")]
    public async Task Rescue_SelectsANestedAggregateLeafAndPreservesTheOriginalFailureAsync()
    {
        var target = new HandledException("target");
        var failure = new AggregateException(
            new AggregateException(new InvalidOperationException("other"), target),
            new ApplicationException("sibling"));
        Exception? projected = null;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception), rescue =>
                {
                    rescue.Handle<HandledException>();
                    rescue.UseExecute(context => projected = context.Exception);
                });
            configuration.UseExecute(_ => throw failure);
        });

        await pipe.SendAsync(new TestPipeContext());

        Assert.Same(failure, projected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "nested-aggregate-leaf-vetoes-broad-rescue")]
    public async Task Rescue_NestedIgnoredLeafVetoesBroadHandleAndRethrowsOriginalAsync()
    {
        var failure = new AggregateException(
            new AggregateException(new InvalidOperationException("other"), new HandledException("excluded")),
            new ApplicationException("sibling"));
        var rescueCount = 0;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception), rescue =>
                {
                    rescue.Handle<Exception>();
                    rescue.Ignore<HandledException>();
                    rescue.UseExecute(_ => rescueCount++);
                });
            configuration.UseExecute(_ => throw failure);
        });

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Same(failure, actual);
        Assert.Equal(0, rescueCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "aggregate-type-predicate-sees-original-exception")]
    public async Task Rescue_AggregatePredicateSeesTheOriginalExceptionOnceAsync()
    {
        var failure = new AggregateException(new InvalidOperationException("inner"));
        var examined = new List<AggregateException>();
        Exception? projected = null;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception), rescue =>
                {
                    rescue.Handle<AggregateException>(exception =>
                    {
                        examined.Add(exception);
                        return ReferenceEquals(exception, failure);
                    });
                    rescue.UseExecute(context => projected = context.Exception);
                });
            configuration.UseExecute(_ => throw failure);
        });

        await pipe.SendAsync(new TestPipeContext());

        Assert.Same(failure, projected);
        Assert.Same(failure, Assert.Single(examined));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "single-inner-aggregate-ignore-vetoes-rescue")]
    public async Task Rescue_IgnoringTheOuterAggregateVetoesBroadHandleAsync()
    {
        var failure = new AggregateException(new InvalidOperationException("inner"));
        var rescueCount = 0;
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception), rescue =>
                {
                    rescue.Handle<Exception>();
                    rescue.Ignore<AggregateException>();
                    rescue.UseExecute(_ => rescueCount++);
                });
            configuration.UseExecute(_ => throw failure);
        });

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Same(failure, actual);
        Assert.Equal(0, rescueCount);
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

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "send-boundary-rejects-null-arguments-before-rescue")]
    public async Task Send_RejectsNullArgumentsBeforeEnteringTheRescuePathAsync()
    {
        IFilter<TestPipeContext> filter = new RescueFilter<TestPipeContext, TestPipeContext>(
            Pipe.Empty<TestPipeContext>(),
            Retry.All(),
            (context, _) => context);

        ArgumentNullException missingContext = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(null!, Pipe.Empty<TestPipeContext>()));
        ArgumentNullException missingNext = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(new TestPipeContext(), null!));

        Assert.Equal("context", missingContext.ParamName);
        Assert.Equal("next", missingNext.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "context-pipe-preserves-the-rescue-projection")]
    public async Task ContextPipe_PreservesTheRescueProjectionAcrossTheSplitBoundaryAsync()
    {
        ITestPipeContext? contextPipeInput = null;
        RescueContext? typedInput = null;
        var source = new TestPipeContext();
        var failure = new HandledException("handled");
        IPipe<ITestPipeContext> pipe = Pipe.New<ITestPipeContext>(configuration =>
        {
            configuration.UseRescue<ITestPipeContext, RescueContext>(
                (context, exception) => new RescueContext(context, exception), rescue =>
                {
                    rescue.ContextPipe.UseExecute(context => contextPipeInput = context);
                    rescue.UseExecute(context => typedInput = context);
                });
            configuration.UseExecute(_ => throw failure);
        });

        await pipe.SendAsync(source);

        Assert.NotNull(typedInput);
        Assert.Same(typedInput, contextPipeInput);
        Assert.Same(source, typedInput.Source);
        Assert.Same(failure, typedInput.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "context-pipe-rejects-null-specification")]
    public void ContextPipe_RejectsANullSpecificationAtRegistration()
    {
        ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() =>
            Pipe.New<ITestPipeContext>(configuration =>
                configuration.UseRescue<ITestPipeContext, RescueContext>(
                    (context, exception) => new RescueContext(context, exception), rescue =>
                        rescue.ContextPipe.AddPipeSpecification(null!))));

        Assert.Equal("specification", actual.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "context-pipe-forwards-inner-validation-failures")]
    public void ContextPipe_RejectsInnerValidationFailuresBeforeApplyingTheSpecification()
    {
        var inner = new InvalidRescueSpecification();

        ConfigurationException actual = Assert.Throws<ConfigurationException>(() =>
            Pipe.New<ITestPipeContext>(configuration =>
                configuration.UseRescue<ITestPipeContext, RescueContext>(
                    (context, exception) => new RescueContext(context, exception), rescue =>
                        rescue.ContextPipe.AddPipeSpecification(inner))));

        ValidationResult result = Assert.Single(actual.Results);
        Assert.Equal("InnerRescue", result.Key);
        Assert.Equal("invalid inner rescue configuration", result.Message);
        Assert.False(inner.Applied);
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

    private sealed class InvalidRescueSpecification : IPipeSpecification<ITestPipeContext>
    {
        public bool Applied { get; private set; }

        public void Apply(IPipeBuilder<ITestPipeContext> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            Applied = true;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure("InnerRescue", "invalid inner rescue configuration");
        }
    }
}
