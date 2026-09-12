using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonIsolationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "independent-configurations")]
    public void IndependentConfigurations_MaterializeDifferentStablePayloadPolicies()
    {
        var snakeCase = new SerializationConfiguration();
        snakeCase.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            return options;
        });
        var kebabCase = new SerializationConfiguration();
        kebabCase.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
            return options;
        });

        ISerialization snakeCollection = snakeCase.CreateSerializerCollection();
        ISerialization kebabCollection = kebabCase.CreateSerializerCollection();

        Assert.Equal("message_id", SinglePropertyName(snakeCollection));
        Assert.Equal("message-id", SinglePropertyName(kebabCollection));
        Assert.Equal("message_id", SinglePropertyName(snakeCollection));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "child-inherits-and-overrides")]
    public void ChildConfiguration_InheritsThenOverridesWithoutMutatingParentOrSibling()
    {
        var parent = new SerializationConfiguration();
        parent.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.WriteIndented = true;
            return options;
        });
        var child = (SerializationConfiguration)parent.CreateSerializationConfiguration();
        var sibling = (SerializationConfiguration)parent.CreateSerializationConfiguration();
        bool? inheritedWriteIndented = null;
        JsonNamingPolicy? inheritedNamingPolicy = null;
        child.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            inheritedWriteIndented = options.WriteIndented;
            inheritedNamingPolicy = options.PropertyNamingPolicy;
            options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
            options.WriteIndented = false;
            return options;
        });

        ISerialization childCollection = child.CreateSerializerCollection();
        ISerialization parentCollection = parent.CreateSerializerCollection();
        ISerialization siblingCollection = sibling.CreateSerializerCollection();

        Assert.True(inheritedWriteIndented);
        Assert.Equal(JsonNamingPolicy.SnakeCaseLower, inheritedNamingPolicy);
        Assert.Equal("message-id", SinglePropertyName(childCollection));
        Assert.Equal("message_id", SinglePropertyName(parentCollection));
        Assert.Equal("message_id", SinglePropertyName(siblingCollection));
        Assert.DoesNotContain('\n', SerializeObject(childCollection));
        Assert.Contains('\n', SerializeObject(parentCollection));
        Assert.Contains('\n', SerializeObject(siblingCollection));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "envelope-and-raw-share-policy")]
    public void EnvelopeAndRawFactories_BindTheSameLocalPayloadPolicy()
    {
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            return options;
        });
        var rawFactory = new SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions.AnyMessageType);
        configuration.AddSerializer(rawFactory, isSerializer: false);
        configuration.AddDeserializer(rawFactory);
        ISerialization serializers = configuration.CreateSerializerCollection();
        IMessageSerializer envelope = serializers.GetMessageSerializer();
        Assert.True(serializers.TryGetMessageSerializer(SystemTextJsonRawMessageSerializer.JsonContentType, out IMessageSerializer? raw));
        Assert.True(serializers.TryGetMessageDeserializer(SystemTextJsonMessageSerializer.JsonContentType, out IMessageDeserializer? envelopeDeserializer));
        Assert.True(serializers.TryGetMessageDeserializer(SystemTextJsonRawMessageSerializer.JsonContentType, out IMessageDeserializer? rawDeserializer));
        var message = new ConfiguredMessage { MessageId = 27 };
        var envelopeContext = new MessageSendContext<ConfiguredMessage>(message) { Serializer = envelope };
        var rawContext = new MessageSendContext<ConfiguredMessage>(message) { Serializer = raw };

        using JsonDocument envelopeDocument = JsonDocument.Parse(envelope.GetMessageBody(envelopeContext).ToArray());
        using JsonDocument rawDocument = JsonDocument.Parse(raw.GetMessageBody(rawContext).ToArray());

        Assert.Equal(27, envelopeDocument.RootElement.GetProperty("message").GetProperty("message_id").GetInt32());
        Assert.Equal(27, rawDocument.RootElement.GetProperty("message_id").GetInt32());
        Assert.False(envelopeDocument.RootElement.GetProperty("message").TryGetProperty("messageId", out _));
        Assert.False(rawDocument.RootElement.TryGetProperty("messageId", out _));
        Assert.Same(envelope, envelopeDeserializer);
        Assert.Same(raw, rawDeserializer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "replacement-defensive-snapshot")]
    public void ReplacementOptions_AreDefensivelyFrozenAtMaterialization()
    {
        var replacement = SystemTextJsonSerializerOptions.CreateDefault();
        replacement.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(_ => replacement);

        ISerialization collection = configuration.CreateSerializerCollection();
        replacement.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;

        Assert.Equal("message_id", SinglePropertyName(collection));
        Assert.Equal(JsonNamingPolicy.KebabCaseLower, replacement.PropertyNamingPolicy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "materialization-closes-configuration")]
    public void Materialization_ClosesEveryConfigurationMutationPath()
    {
        var configuration = new SerializationConfiguration();
        _ = configuration.CreateSerializerCollection();

        ConfigurationException configure = Assert.Throws<ConfigurationException>(() =>
            configuration.ConfigureSystemTextJsonSerializerOptions(options => options));
        ConfigurationException clear = Assert.Throws<ConfigurationException>(configuration.Clear);
        ConfigurationException serializer = Assert.Throws<ConfigurationException>(() =>
            configuration.AddSerializer(new SystemTextJsonMessageSerializerFactory()));

        Assert.Equal(
            "Serialization for bus 'unknown': The serializer collection was already created. Correct the named configuration before starting the host.",
            configure.Message);
        Assert.Equal(configure.Message, clear.Message);
        Assert.Equal(configure.Message, serializer.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "null-transform-result")]
    public void NullTransformResult_FailsDuringMaterializationWithoutPublishingACollection()
    {
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(_ => null!);

        ConfigurationException first = Assert.Throws<ConfigurationException>(configuration.CreateSerializerCollection);
        ConfigurationException second = Assert.Throws<ConfigurationException>(configuration.CreateSerializerCollection);

        Assert.Contains("returned null", first.Message, StringComparison.Ordinal);
        Assert.Same(first, second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "immutable-construction-boundary")]
    public void MutableOptions_AreRejectedByEnvelopeAndRawSerializerConstructors()
    {
        var options = SystemTextJsonSerializerOptions.CreateDefault();

        ArgumentException envelope = Assert.Throws<ArgumentException>(() => new SystemTextJsonMessageSerializer(options));
        ArgumentException raw = Assert.Throws<ArgumentException>(() => new SystemTextJsonRawMessageSerializer(options));

        Assert.Equal("options", envelope.ParamName);
        Assert.Equal("serializerOptions", raw.ParamName);
        Assert.Contains("immutable", envelope.Message, StringComparison.Ordinal);
        Assert.Contains("immutable", raw.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "content-types-are-defensive-values")]
    public void PublicContentTypes_CannotMutateSerializerOrGlobalMediaTypes()
    {
        ContentType envelopeGlobal = SystemTextJsonMessageSerializer.JsonContentType;
        ContentType rawGlobal = SystemTextJsonRawMessageSerializer.JsonContentType;
        var custom = new ContentType("application/vnd.example+json");
        var envelope = new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options, custom);
        var raw = new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options);
        envelopeGlobal.MediaType = "application/mutated-envelope";
        rawGlobal.MediaType = "application/mutated-raw";
        custom.MediaType = "application/mutated-custom";
        envelope.ContentType.MediaType = "application/mutated-return-value";
        raw.ContentType.MediaType = "application/mutated-return-value";

        Assert.Equal(SystemTextJsonMessageSerializer.JsonMediaType, SystemTextJsonMessageSerializer.JsonContentType.MediaType);
        Assert.Equal(SystemTextJsonRawMessageSerializer.JsonMediaType, SystemTextJsonRawMessageSerializer.JsonContentType.MediaType);
        Assert.Equal("application/vnd.example+json", envelope.ContentType.MediaType);
        Assert.Equal(SystemTextJsonRawMessageSerializer.JsonMediaType, raw.ContentType.MediaType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "factory-binding-required")]
    public void JsonFactories_RejectDirectUseBeforeConfigurationBinding()
    {
        var envelopeFactory = new SystemTextJsonMessageSerializerFactory();
        var rawFactory = new SystemTextJsonRawMessageSerializerFactory();

        ConfigurationException envelopeSerializer = Assert.Throws<ConfigurationException>(envelopeFactory.CreateSerializer);
        ConfigurationException envelopeDeserializer = Assert.Throws<ConfigurationException>(envelopeFactory.CreateDeserializer);
        ConfigurationException rawSerializer = Assert.Throws<ConfigurationException>(rawFactory.CreateSerializer);
        ConfigurationException rawDeserializer = Assert.Throws<ConfigurationException>(rawFactory.CreateDeserializer);

        Assert.Contains("must be bound", envelopeSerializer.Message, StringComparison.Ordinal);
        Assert.Equal(envelopeSerializer.Message, envelopeDeserializer.Message);
        Assert.Contains("must be bound", rawSerializer.Message, StringComparison.Ordinal);
        Assert.Equal(rawSerializer.Message, rawDeserializer.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-JSON-STABILITY", "payload-policy-does-not-leak")]
    public void PayloadPolicy_CannotAlterTheStableMetadataCodec()
    {
        var metadata = new ConfiguredMessage { MessageId = 27 };
        string before = ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(metadata).GetRequiredTransportText();
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
            options.WriteIndented = true;
            return options;
        });

        ISerialization payload = configuration.CreateSerializerCollection();
        string after = ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(metadata).GetRequiredTransportText();

        Assert.Equal(before, after);
        using JsonDocument metadataDocument = JsonDocument.Parse(after);
        Assert.Equal(27, metadataDocument.RootElement.GetProperty("messageId").GetInt32());
        Assert.Equal("message-id", SinglePropertyName(payload));
    }

    private static string SinglePropertyName(ISerialization serialization)
    {
        using JsonDocument document = JsonDocument.Parse(SerializeObject(serialization));
        return Assert.Single(document.RootElement.EnumerateObject()).Name;
    }

    private static string SerializeObject(ISerialization serialization) =>
        Assert.IsAssignableFrom<IObjectDeserializer>(serialization.GetMessageSerializer())
            .SerializeObject(new ConfiguredMessage { MessageId = 27 })
            .GetRequiredTransportText();

    public sealed class ConfiguredMessage
    {
        public int MessageId { get; init; }
    }
}
