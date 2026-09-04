using ViciOne.ServiceBus.Initializers.Variables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryVariableTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-VARIABLE", "guid-and-string-results")]
    public async Task InitializerVariable_ProvidesExactGuidAndStringRepresentationsAsync()
    {
        var expected = new Guid("5b68c65a-5083-46b5-8e88-f4de120be46f");
        var reader = PropertyProviderTestContext.For(new VariableInput(new IdVariable(expected)));

        Guid id = await reader.ReadAsync<Guid>(nameof(VariableInput.Value));
        string text = await reader.ReadAsync<string>(nameof(VariableInput.Value));

        Assert.Equal(expected, id);
        Assert.Equal(expected.ToString(), text);
    }

    private sealed record VariableInput(IdVariable Value);
}
