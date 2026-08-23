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

        bool found = factory.TryGetPropertyConverter(out IPropertyConverter<DictionaryContract, object> converter);

        Assert.True(found);
        Assert.NotNull(converter);
    }

    public interface DictionaryContract
    {
        int Id { get; }
    }
}
