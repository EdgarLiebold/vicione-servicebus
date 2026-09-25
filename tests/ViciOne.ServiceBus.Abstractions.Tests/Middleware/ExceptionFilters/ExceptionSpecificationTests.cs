using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware.ExceptionFilters;

public sealed class ExceptionSpecificationTests
{
    [Theory]
    [InlineData("conflict", "There was a conflict", true)]
    [InlineData("confusion", "There was a conflict", false)]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "type-and-predicate")]
    public void TypedPredicate_DecidesWhetherTheExceptionMatches(
        string requiredText,
        string message,
        bool expected)
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>(exception => exception.Message.Contains(requiredText, StringComparison.Ordinal));

        Assert.Equal(expected, specification.Matches(new Exception(message)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "type-only")]
    public void TypeOnlyRegistration_MatchesThatExceptionType()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>();

        Assert.True(specification.Matches(new Exception()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "derived-exception")]
    public void BaseTypeRegistration_MatchesDerivedExceptions()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>(exception => exception.Message.Contains("value", StringComparison.Ordinal));

        Assert.True(specification.Matches(new ArgumentException("invalid value", "value")));
    }

    [Fact]
    public void BroadPredicate_ExaminesAggregateInnersWhenTheAggregateDoesNotPass()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>(exception => exception.Message == "retryable");
        var aggregate = new AggregateException(
            new InvalidOperationException("other"),
            new TimeoutException("retryable"));

        Assert.True(specification.Matches(aggregate));
    }

    [Fact]
    public void IgnoredAggregateInner_VetoesAnOtherwiseHandledException()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>();
        specification.Ignore<Exception>(exception => exception is TimeoutException && exception.Message == "retryable");
        var aggregate = new AggregateException(
            new InvalidOperationException("other"),
            new TimeoutException("retryable"));

        Assert.False(specification.Matches(aggregate));
        Assert.True(specification.Matches(new AggregateException(
            new InvalidOperationException("other"),
            new TimeoutException("fatal"))));
    }

    [Fact]
    public void TypedPredicate_ExaminesDirectAggregateInnerBeforeItsBaseException()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<InvalidOperationException>(exception => exception.Message == "retryable");
        var aggregate = new AggregateException(
            new InvalidOperationException("retryable", new Exception("root cause")),
            new TimeoutException("unrelated"));

        Assert.True(specification.Matches(aggregate));
    }

    [Fact]
    public void Predicate_ExaminesDistinctRootCauseWhenDirectAggregateInnerDoesNotPass()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>(exception => exception.Message == "root cause");
        var aggregate = new AggregateException(
            new InvalidOperationException("outer", new Exception("root cause")),
            new TimeoutException("unrelated"));

        Assert.True(specification.Matches(aggregate));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "immutable-snapshot")]
    public void Snapshot_IsDetachedFromLaterConfigurationAndCallerOwnedTypeArrays()
    {
        Type[] handledTypes = [typeof(InvalidOperationException)];
        var specification = new TestExceptionSpecification();
        specification.Handle(handledTypes);
        IExceptionFilter snapshot = specification.CreateSnapshot();

        handledTypes[0] = typeof(ArgumentException);
        specification.Ignore<InvalidOperationException>();

        Assert.True(snapshot.Match(new InvalidOperationException("captured")));
        Assert.False(snapshot.Match(new ArgumentException("not captured")));
        Assert.False(specification.Matches(new InvalidOperationException("now ignored")));
    }

    private sealed class TestExceptionSpecification : ExceptionSpecification
    {
        public bool Matches(Exception exception) => Filter.Match(exception);

        public IExceptionFilter CreateSnapshot() => CreateFilterSnapshot();
    }
}
