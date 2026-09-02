using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
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

[Collection(SystemTextJsonGlobalOptionsCollection.Name)]
public sealed class SystemTextJsonGlobalConfigurationTests : IDisposable
{
    private readonly JsonSerializerOptions _original = SystemTextJsonMessageSerializer.Options;

    public SystemTextJsonGlobalConfigurationTests()
    {
        SystemTextJsonMessageSerializer.Options = new JsonSerializerOptions(_original)
        {
            WriteIndented = false,
        };
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-GLOBAL-OPTIONS", "mutated-result")]
    public void MutatedCallbackCopy_BecomesTheSharedOptions()
    {
        Configure(callbackOptions =>
        {
            callbackOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            return callbackOptions;
        });

        Assert.Same(JsonNamingPolicy.CamelCase, SystemTextJsonMessageSerializer.Options.PropertyNamingPolicy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-GLOBAL-OPTIONS", "replacement-result")]
    public void CallbackReplacement_BecomesTheExactSharedOptionsInstance()
    {
        var replacement = new JsonSerializerOptions { WriteIndented = true };

        Configure(_ => replacement);

        Assert.Same(replacement, SystemTextJsonMessageSerializer.Options);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-GLOBAL-OPTIONS", "defensive-copy")]
    public void CallbackReceivesADefensiveCopyOfTheSharedOptions()
    {
        JsonSerializerOptions before = SystemTextJsonMessageSerializer.Options;
        JsonSerializerOptions? received = null;

        Configure(callbackOptions =>
        {
            received = callbackOptions;
            callbackOptions.WriteIndented = true;
            return callbackOptions;
        });

        Assert.NotNull(received);
        Assert.NotSame(before, received);
        Assert.False(before.WriteIndented);
        Assert.True(SystemTextJsonMessageSerializer.Options.WriteIndented);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-GLOBAL-OPTIONS", "null-result")]
    public void NullCallbackResult_IsRejectedWithoutReplacingTheSharedOptions()
    {
        JsonSerializerOptions before = SystemTextJsonMessageSerializer.Options;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => Configure(_ => null!));

        Assert.Contains("returned null", exception.Message, StringComparison.Ordinal);
        Assert.Same(before, SystemTextJsonMessageSerializer.Options);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-GLOBAL-OPTIONS", "no-callback")]
    public void MissingCallback_LeavesTheSharedOptionsInstanceUnchanged()
    {
        JsonSerializerOptions before = SystemTextJsonMessageSerializer.Options;

        Configure(null);

        Assert.Same(before, SystemTextJsonMessageSerializer.Options);
    }

    public void Dispose()
    {
        SystemTextJsonMessageSerializer.Options = _original;
    }

    private static void Configure(Func<JsonSerializerOptions, JsonSerializerOptions>? configure)
    {
        Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.ConfigureJsonSerializerOptions(configure));
    }
}
