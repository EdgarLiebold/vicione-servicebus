using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-DICTIONARY-CONVERSION", "object-to-message-contract")]
    public void DictionaryInputFactory_ProvidesAnObjectToMessageContractConverter()
    {
        var factory = new PropertyProviderFactory<IDictionary<string, object>>();

        bool found = factory.TryGetPropertyConverter(out IPropertyConverter<DictionaryContract, object>? converter);

        Assert.True(found);
        Assert.NotNull(converter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-FACTORY", "unsupported-source-target-pair")]
    public void UnsupportedSourceTargetPair_ReturnsFalseAndNoProvider()
    {
        var factory = new PropertyProviderFactory<UnsupportedInput>();
        var property = typeof(UnsupportedInput).GetProperty(nameof(UnsupportedInput.Value));

        Assert.NotNull(property);
        bool found = factory.TryGetPropertyProvider(property, out IPropertyProvider<UnsupportedInput, ExceptionInfo>? provider);

        Assert.False(found);
        Assert.Null(provider);
    }

    public interface DictionaryContract
    {
        int Id { get; }
    }

    public sealed record UnsupportedInput(int Value);
}
