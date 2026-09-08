using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Configuration;

public sealed class MessagePackConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PUBLIC-API", "minimal-greenfield-surface")]
    public void PublicApi_ContainsOnlyCompositionAndTheAdvancedFactory()
    {
        string[] exportedTypes = typeof(MessagePackConfigurationExtensions).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ViciOne.ServiceBus.MessagePack.MessagePackConfigurationExtensions",
                "ViciOne.ServiceBus.MessagePack.MessagePackSerializerFactory",
            ],
            exportedTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "missing-owner-fails-at-extension-boundary")]
    public void ConfigurationExtensions_RejectMissingConfiguratorsWithExactOwnership()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackSerializer(
                (IReceiveEndpointConfigurator)null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackSerializer(
                (IBusFactoryConfigurator)null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackDeserializer(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DEPENDENCIES", "no-optional-capability-coupling")]
    public void ProductAssembly_DoesNotReferenceOptionalCourierOrJobServiceCapabilities()
    {
        string[] references = typeof(MessagePackSerializerFactory).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.DoesNotContain("ViciOne.ServiceBus.Courier", references);
        Assert.DoesNotContain("ViciOne.ServiceBus.JobService", references);
    }
}
