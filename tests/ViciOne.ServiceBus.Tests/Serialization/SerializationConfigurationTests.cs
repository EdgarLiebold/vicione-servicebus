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
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "empty-registration-requires-both-directions")]
    public void ClearedConfiguration_ReportsBothMissingDirectionsAndAcceptsRestoredJson()
    {
        var configuration = new SerializationConfiguration();
        configuration.Clear();

        ValidationResult[] failures = configuration.Validate().ToArray();

        Assert.Equal(["Serializers", "Deserializers"], failures.Select(failure => failure.Key));
        Assert.All(failures, failure => Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition));
        Assert.Contains(failures, failure => failure.Key == "Serializers"
            && failure.Message == "must specify at least one serializer");
        Assert.Contains(failures, failure => failure.Key == "Deserializers"
            && failure.Message == "must specify at least one deserializer");

        IEnumerable<ValidationResult> deferred = configuration.Validate();
        var json = new SystemTextJsonMessageSerializerFactory();
        configuration.AddSerializer(json);
        ValidationResult remaining = Assert.Single(deferred);
        Assert.Equal("Deserializers", remaining.Key);
        Assert.Equal(ValidationResultDisposition.Failure, remaining.Disposition);
        configuration.AddDeserializer(json, isDefault: true);
        Assert.Empty(configuration.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERIALIZATION-CONFIGURATION", "multiple-registrations-require-explicit-selection")]
    public void MultipleFormats_RequireIndependentSelectionsBeforeCreatingCollection()
    {
        var configuration = new SerializationConfiguration();
        configuration.Clear();
        var json = new SystemTextJsonMessageSerializerFactory();
        var raw = new SystemTextJsonRawMessageSerializerFactory();
        configuration.AddSerializer(json, isSerializer: false);
        configuration.AddSerializer(raw, isSerializer: false);
        configuration.AddDeserializer(json);
        configuration.AddDeserializer(raw);

        ValidationResult[] failures = configuration.Validate().ToArray();

        Assert.Equal(["SerializerContentType", "DefaultContentType"], failures.Select(failure => failure.Key));
        Assert.All(failures, failure => Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition));
        Assert.Contains(failures, failure => failure.Key == "SerializerContentType"
            && failure.Message == "must be specified when more than one serializer is supported");
        Assert.Contains(failures, failure => failure.Key == "DefaultContentType"
            && failure.Message == "must be specified when more than one deserializer is supported");

        configuration.SerializerContentType = raw.ContentType;
        configuration.DefaultContentType = json.ContentType;
        Assert.Empty(configuration.Validate());

        var collection = configuration.CreateSerializerCollection();
        Assert.Equal(json.ContentType.MediaType, collection.DefaultContentType.MediaType);
        Assert.Equal(raw.ContentType.MediaType, collection.GetMessageSerializer().ContentType.MediaType);
        Assert.Equal(json.ContentType.MediaType, collection.GetMessageDeserializer().ContentType.MediaType);
    }

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
