using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class ExceptionFilterContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-FILTER", "typed-filter-traverses-inner-exception-chain")]
    public void TypedFilter_EvaluatesTheMatchingInnerExceptionExactlyOnce()
    {
        var predicateCalls = 0;
        var expected = new ExpectedException();
        IExceptionFilter filter = Retry.Filter<ExpectedException>(exception =>
        {
            Interlocked.Increment(ref predicateCalls);
            return ReferenceEquals(expected, exception);
        });
        var wrapped = new InvalidOperationException("outer", new ApplicationException("middle", expected));

        bool handled = filter.Match(wrapped);

        Assert.True(handled);
        Assert.Equal(1, predicateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-FILTER", "typed-filter-leaves-other-exceptions-enabled")]
    public void TypedFilter_LeavesUnmatchedExceptionTypesEnabledWithoutCallingThePredicate()
    {
        var predicateCalls = 0;
        IExceptionFilter filter = Retry.Filter<ExpectedException>(_ =>
        {
            Interlocked.Increment(ref predicateCalls);
            return false;
        });

        bool handled = filter.Match(new InvalidOperationException("unrelated"));

        Assert.True(handled);
        Assert.Equal(0, predicateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-FILTER", "public-filter-input-boundaries")]
    public void PublicFilterFactories_RejectNullPredicatesAndInvalidExceptionTypes()
    {
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() => Retry.Filter<ExpectedException>(null!)).ParamName);
        Assert.Equal("exceptionTypes", Assert.Throws<ArgumentNullException>(() => Retry.Except(null!)).ParamName);
        Assert.Equal("exceptionTypes", Assert.Throws<ArgumentException>(() => Retry.Selected(typeof(string))).ParamName);
        Assert.Equal("exceptionTypes", Assert.Throws<ArgumentException>(() => Retry.Except([typeof(ExpectedException), null!])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-FILTER", "filters-reject-null-runtime-exceptions")]
    public void EveryFilter_RejectsANullRuntimeException()
    {
        IExceptionFilter[] filters =
        [
            Retry.All(),
            Retry.Selected<ExpectedException>(),
            Retry.Except<ExpectedException>(),
            Retry.Filter<ExpectedException>(_ => true)
        ];

        Assert.All(filters, filter =>
            Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => filter.Match(null!)).ParamName));
    }

    private sealed class ExpectedException : Exception;
}
