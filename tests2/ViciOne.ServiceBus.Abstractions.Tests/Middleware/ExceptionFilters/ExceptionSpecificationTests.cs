namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware.ExceptionFilters;

using Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

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

    private sealed class TestExceptionSpecification : ExceptionSpecification
    {
        public bool Matches(Exception exception) => Filter.Match(exception);
    }
}
