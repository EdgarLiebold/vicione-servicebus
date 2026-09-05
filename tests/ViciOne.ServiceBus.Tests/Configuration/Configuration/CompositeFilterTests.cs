using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class CompositeFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COMPOSITE-FILTER", "get-only-predicate-owners-and-complete-truth-table")]
    public void Filter_ExposesGetOnlyPredicatesAndAppliesIncludesBeforeExcludes()
    {
        PropertyInfo includes = typeof(CompositeFilter<int>).GetProperty(nameof(CompositeFilter<int>.Includes))!;
        PropertyInfo excludes = typeof(CompositeFilter<int>).GetProperty(nameof(CompositeFilter<int>.Excludes))!;
        var filter = new CompositeFilter<int>();

        Assert.Null(includes.SetMethod);
        Assert.Null(excludes.SetMethod);
        Assert.NotNull(typeof(CompositePredicate<int>).GetMethod("DoesNotMatchAny"));
        Assert.Null(typeof(CompositePredicate<int>).GetMethod("DoesNotMatcheAny"));

        filter.Includes.Add(value => value > 0);
        filter.Excludes.Add(value => value % 2 == 0);

        Assert.True(filter.Matches(1));
        Assert.False(filter.Matches(-1));
        Assert.False(filter.Matches(2));
    }
}
