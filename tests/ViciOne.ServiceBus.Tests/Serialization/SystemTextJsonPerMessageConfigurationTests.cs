using System.Text.Json;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonPerMessageConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PER-MESSAGE-OPTIONS", "replacement-result")]
    public void CallbackReplacement_IsUsedForTheConfiguredMessageType()
    {
        var options = new JsonSerializerOptions();

        options.SetMessageSerializerOptions<ConfiguredMessage>(
            _ => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        AssertSerializedPropertyName(options, "messageId");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PER-MESSAGE-OPTIONS", "mutated-result")]
    public void MutatedCallbackInput_IsUsedForTheConfiguredMessageType()
    {
        var options = new JsonSerializerOptions();

        options.SetMessageSerializerOptions<ConfiguredMessage>(callbackOptions =>
        {
            callbackOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            return callbackOptions;
        });

        AssertSerializedPropertyName(options, "messageId");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PER-MESSAGE-OPTIONS", "null-result")]
    public void NullCallbackResult_IsRejectedWithoutInstallingAConverter()
    {
        var options = new JsonSerializerOptions();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => options.SetMessageSerializerOptions<ConfiguredMessage>(_ => null!));

        Assert.Contains("returned null", exception.Message, StringComparison.Ordinal);
        Assert.Empty(options.Converters);
    }

    private static void AssertSerializedPropertyName(JsonSerializerOptions options, string expectedName)
    {
        string json = JsonSerializer.Serialize(new ConfiguredMessage { MessageId = 27 }, options);
        using JsonDocument document = JsonDocument.Parse(json);

        JsonProperty property = Assert.Single(document.RootElement.EnumerateObject());
        Assert.Equal(expectedName, property.Name);
        Assert.Equal(27, property.Value.GetInt32());
    }

    public sealed class ConfiguredMessage
    {
        public int MessageId { get; init; }
    }
}
