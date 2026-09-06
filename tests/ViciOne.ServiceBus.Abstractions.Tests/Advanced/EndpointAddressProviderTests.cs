using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class EndpointAddressProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-ADDRESS-PROVIDER", "nullable-result-contract")]
    public void ProviderResult_IsExplicitlyNullable()
    {
        MethodInfo invoke = typeof(EndpointAddressProvider).GetMethod("Invoke")
            ?? throw new InvalidOperationException("The delegate invoke method is missing.");
        NullabilityInfo result = new NullabilityInfoContext().Create(invoke.ReturnParameter);

        Assert.Empty(invoke.GetParameters());
        Assert.Equal(typeof(Uri), invoke.ReturnType);
        Assert.Equal(NullabilityState.Nullable, result.ReadState);
    }
}
