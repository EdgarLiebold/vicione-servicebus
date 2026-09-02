using System.Net.Mime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SerializationConfigurationTests
{
    private const string UnregisteredMediaType = "application/vnd.vicione.unregistered+json";

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "registered-serializer")]
    public void RegisteredSerializerContentType_IsAccepted()
    {
        var configuration = new SerializationConfiguration();

        Assert.Empty(configuration.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "unregistered-serializer")]
    public void UnregisteredSerializerContentType_IsRejectedByItsExactValidationMember()
    {
        var configuration = new SerializationConfiguration
        {
            SerializerContentType = new ContentType(UnregisteredMediaType),
        };

        ValidationResult result = Assert.Single(configuration.Validate());

        Assert.Equal(ValidationResultDisposition.Failure, result.Disposition);
        Assert.Equal("SerializerContentType", result.Key);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "unregistered-default")]
    public void UnregisteredDefaultContentType_IsRejectedByItsExactValidationMember()
    {
        var configuration = new SerializationConfiguration
        {
            DefaultContentType = new ContentType(UnregisteredMediaType),
        };

        ValidationResult result = Assert.Single(configuration.Validate());

        Assert.Equal(ValidationResultDisposition.Failure, result.Disposition);
        Assert.Equal("DefaultContentType", result.Key);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "media-type-case-insensitive")]
    public void RegisteredSerializerContentType_IsMatchedCaseInsensitively()
    {
        var configuration = new SerializationConfiguration
        {
            SerializerContentType = new ContentType(
                SystemTextJsonMessageSerializer.JsonContentType.MediaType.ToUpperInvariant()),
        };

        Assert.Empty(configuration.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "child-unregistered-serializer")]
    public void ChildConfiguration_RejectsAnUnregisteredSerializerContentType()
    {
        var parent = new SerializationConfiguration();
        var child = Assert.IsType<SerializationConfiguration>(parent.CreateSerializationConfiguration());
        child.SerializerContentType = new ContentType(UnregisteredMediaType);

        ValidationResult result = Assert.Single(child.Validate());

        Assert.Equal(ValidationResultDisposition.Failure, result.Disposition);
        Assert.Equal("SerializerContentType", result.Key);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "child-inherited-serializer")]
    public void ChildConfiguration_AcceptsItsParentsRegisteredSerializerContentType()
    {
        var parent = new SerializationConfiguration();
        var child = Assert.IsType<SerializationConfiguration>(parent.CreateSerializationConfiguration());
        child.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;

        Assert.Empty(child.Validate());
    }
}
