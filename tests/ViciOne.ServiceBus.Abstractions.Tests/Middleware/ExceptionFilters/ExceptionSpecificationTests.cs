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
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "t122-nested-aggregate-leaf-is-handled-by-type")]
    public void TypeRegistration_HandlesALeafInsideANestedMultiAggregate()
    {
        var target = new TimeoutException("retryable");
        var nested = new AggregateException(new InvalidOperationException("sibling"), target);
        var root = new AggregateException(nested, new ApplicationException("outer sibling"));
        var specification = new TestExceptionSpecification();
        specification.Handle<TimeoutException>();

        Assert.True(specification.Matches(root));
        Assert.True(specification.Matches(new InvalidOperationException("wrapper", root)));
        Assert.True(specification.CreateSnapshot().Match(root));
        Assert.False(specification.Matches(new AggregateException(
            new AggregateException(new InvalidOperationException("one"), new ApplicationException("two")),
            new ArgumentException("three"))));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "t122-nested-aggregate-leaf-is-excluded")]
    public void IgnoreRegistration_VetoesALeafInsideANestedMultiAggregate()
    {
        var specification = new TestExceptionSpecification();
        specification.Handle<Exception>();
        specification.Ignore<TimeoutException>();
        var target = new TimeoutException("terminal");
        var selected = new AggregateException(
            new AggregateException(new InvalidOperationException("other"), target),
            new ApplicationException("sibling"));
        var unselected = new AggregateException(
            new AggregateException(new InvalidOperationException("other"), new ArgumentException("different")),
            new ApplicationException("sibling"));

        Assert.False(specification.Matches(selected));
        Assert.True(specification.Matches(unselected));

        var predicateCalls = new List<TimeoutException>();
        var predicateSpecification = new TestExceptionSpecification();
        predicateSpecification.Handle<Exception>();
        predicateSpecification.Ignore<TimeoutException>(exception =>
        {
            predicateCalls.Add(exception);
            return ReferenceEquals(target, exception);
        });
        Assert.False(predicateSpecification.Matches(selected));
        Assert.Same(target, Assert.Single(predicateCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "t122-nested-aggregate-predicate-receives-exact-leaf-once")]
    public void TypedPredicate_EvaluatesTheNestedLeafExactlyOnce()
    {
        var target = new TimeoutException("retryable");
        var nested = new AggregateException(new TimeoutException("other"), target);
        var root = new AggregateException(nested, new ApplicationException("sibling"));
        var calls = new List<TimeoutException>();
        var specification = new TestExceptionSpecification();
        specification.Handle<TimeoutException>(exception =>
        {
            calls.Add(exception);
            return ReferenceEquals(target, exception);
        });

        Assert.True(specification.Matches(root));
        Assert.Collection(calls,
            first => Assert.Same(nested.InnerExceptions[0], first),
            second => Assert.Same(target, second));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "t122-shared-aggregate-leaf-is-evaluated-once")]
    public void SharedLeaf_DoesNotInvokeThePredicateTwiceAcrossAggregateBranches()
    {
        var shared = new TimeoutException("shared");
        var root = new AggregateException(
            new AggregateException(shared, new InvalidOperationException("sibling")),
            shared);
        var calls = 0;
        var specification = new TestExceptionSpecification();
        specification.Handle<TimeoutException>(exception =>
        {
            Assert.Same(shared, exception);
            calls++;
            return false;
        });

        Assert.False(specification.Matches(root));
        Assert.Equal(1, calls);

        var aggregateSpecification = new TestExceptionSpecification();
        aggregateSpecification.Handle<AggregateException>();
        Assert.True(aggregateSpecification.Matches(new InvalidOperationException("outer", root)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "t122-custom-base-exception-remains-visible")]
    public void CustomBaseException_StillControlsTypePredicateAndIgnoreSelection()
    {
        var target = new TimeoutException("redirected base");
        var wrapper = new RedirectingException(target);
        var typeSpecification = new TestExceptionSpecification();
        typeSpecification.Handle<TimeoutException>();
        Assert.True(typeSpecification.Matches(wrapper));

        var predicateCalls = new List<TimeoutException>();
        var predicateSpecification = new TestExceptionSpecification();
        predicateSpecification.Handle<TimeoutException>(exception =>
        {
            predicateCalls.Add(exception);
            return ReferenceEquals(target, exception);
        });
        Assert.True(predicateSpecification.Matches(wrapper));
        Assert.Same(target, Assert.Single(predicateCalls));

        var ignoreSpecification = new TestExceptionSpecification();
        ignoreSpecification.Handle<Exception>();
        ignoreSpecification.Ignore<TimeoutException>();
        Assert.False(ignoreSpecification.Matches(wrapper));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-FILTER", "t122-shared-wrapper-base-is-checked-after-structural-visit")]
    public void SharedWrapper_BaseOverrideIsCheckedAfterAnEarlierStructuralVisit()
    {
        var target = new TimeoutException("redirected base");
        var shared = new RedirectingException(target, new ApplicationException("inner tail"));
        var root = new AggregateException(new ApplicationException("first", shared), shared);

        var typeSpecification = new TestExceptionSpecification();
        typeSpecification.Handle<TimeoutException>();
        Assert.True(typeSpecification.Matches(root));

        var calls = new List<TimeoutException>();
        var predicateSpecification = new TestExceptionSpecification();
        predicateSpecification.Handle<TimeoutException>(exception =>
        {
            calls.Add(exception);
            return ReferenceEquals(exception, target);
        });
        Assert.True(predicateSpecification.Matches(root));
        Assert.Same(target, Assert.Single(calls));
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

    private sealed class RedirectingException(Exception target, Exception? inner = null)
        : Exception("redirected", inner)
    {
        public override Exception GetBaseException() => target;
    }
}
