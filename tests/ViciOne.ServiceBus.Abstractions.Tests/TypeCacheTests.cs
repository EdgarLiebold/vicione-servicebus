namespace ViciOne.ServiceBus.Abstractions.Tests;

using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class TypeCacheTests
{
    [Theory]
    [InlineData(typeof(List<>), "System.Collections.Generic.List<>")]
    [InlineData(typeof(Dictionary<,>), "System.Collections.Generic.Dictionary<,>")]
    [RequirementCoverage("REQ-VSB-TYPE-NAME", "open-generic-definition")]
    public void GetShortName_FormatsOpenGenericDefinitionsWithoutActivation(Type type, string expected)
    {
        Assert.Equal(expected, TypeCache.GetShortName(type));
    }
}
