using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.Advanced.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

public sealed class NewIdStaticFacadeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-STATIC-FACADE", "default-generator-has-no-process-global-mutators")]
    public void StaticFacade_ExposesNoProcessGlobalConfigurationMutators()
    {
        string[] publicStaticMethods = typeof(NewIdValue)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.DoesNotContain("SetGenerator", publicStaticMethods);
        Assert.DoesNotContain("SetWorkerIdProvider", publicStaticMethods);
        Assert.DoesNotContain("SetProcessIdProvider", publicStaticMethods);
        Assert.DoesNotContain("SetTickProvider", publicStaticMethods);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-STATIC-FACADE", "parallel-default-generation-is-unique")]
    public void StaticFacade_ProducesUniqueValuesUnderParallelLoad()
    {
        const int Count = 100_000;
        var ids = new NewIdValue[Count];

        Parallel.For(
            0,
            Count,
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            index => ids[index] = NewIdValue.Next());

        Assert.Equal(Count, ids.Distinct().Count());
    }
}
