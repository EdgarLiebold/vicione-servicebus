using System.Text.Json;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureMessageTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-message-defaults-are-usable")]
    public void DefaultConstruction_ProducesUsableEmptyStorage()
    {
        var message = new FutureMessage();

        Assert.Empty(message.Message);
        Assert.Empty(message.SupportedMessageTypes);
        Assert.False(message.HasMessageType<StoredContract>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-message-contract-matching")]
    public void ContractMatching_IsOrdinalCaseInsensitiveAndRejectsUnknownContracts()
    {
        string contractUrn = MessageUrn.ForTypeString<StoredContract>().ToUpperInvariant();
        var message = new FutureMessage(
            new Dictionary<string, object> { [nameof(StoredContract.Value)] = "value" },
            [contractUrn]);

        Assert.True(message.HasMessageType(typeof(StoredContract)));
        Assert.True(message.HasMessageType<StoredContract>());
        Assert.False(message.HasMessageType<OtherContract>());
        Assert.Equal("value", message.Message[nameof(StoredContract.Value)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-message-required-inputs")]
    public void RequiredStorageInputs_RejectNullAtEveryPublicBoundary()
    {
        var message = new FutureMessage();

        var missingMessage = Assert.Throws<ArgumentNullException>(() => new FutureMessage(null!, []));
        var missingTypes = Assert.Throws<ArgumentNullException>(() => new FutureMessage(new Dictionary<string, object>(), null!));
        var missingMessageSetter = Assert.Throws<ArgumentNullException>(() => message.Message = null!);
        var missingTypesSetter = Assert.Throws<ArgumentNullException>(() => message.SupportedMessageTypes = null!);
        var missingType = Assert.Throws<ArgumentNullException>(() => message.HasMessageType(null!));

        Assert.Equal("message", missingMessage.ParamName);
        Assert.Equal("supportedMessageTypes", missingTypes.ParamName);
        Assert.Equal("value", missingMessageSetter.ParamName);
        Assert.Equal("value", missingTypesSetter.ParamName);
        Assert.Equal("messageType", missingType.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-message-is-a-detached-read-only-snapshot")]
    public void StoredMessage_DetachesConstructorAndSetterInputsAndExposesReadOnlyCollections()
    {
        var constructorValues = new Dictionary<string, object> { ["Value"] = "constructor" };
        string[] constructorTypes = [MessageUrn.ForTypeString<StoredContract>()];
        var message = new FutureMessage(constructorValues, constructorTypes);
        constructorValues["Value"] = "mutated";
        constructorTypes[0] = "mutated";

        var setterValues = new Dictionary<string, object> { ["Value"] = "setter" };
        string[] setterTypes = [MessageUrn.ForTypeString<OtherContract>()];
        message.Message = setterValues;
        message.SupportedMessageTypes = setterTypes;
        setterValues["Value"] = "mutated";
        setterTypes[0] = "mutated";

        Assert.Equal("setter", message.Message["Value"]);
        Assert.Equal([MessageUrn.ForTypeString<OtherContract>()], message.SupportedMessageTypes);
        IDictionary<string, object> dictionary = Assert.IsAssignableFrom<IDictionary<string, object>>(message.Message);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("Other", "value"));
        Assert.False(message.SupportedMessageTypes is string[]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("contract-name")]
    [InlineData("https://example.test/contracts/message")]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-message-contract-names-are-valid")]
    public void SupportedMessageTypes_RejectInvalidContractNames(string? value)
    {
        var message = new FutureMessage();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => message.SupportedMessageTypes = [value!]);

        Assert.Equal("value", exception.ParamName);
        Assert.Empty(message.SupportedMessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-message-json-round-trip")]
    public void JsonRoundTrip_RehydratesDetachedReadOnlyStorage()
    {
        var original = new FutureMessage(
            new Dictionary<string, object> { [nameof(StoredContract.Value)] = "stored" },
            [MessageUrn.ForTypeString<StoredContract>()]);

        string json = JsonSerializer.Serialize(original, ServiceBusMetadataJson.Options);
        FutureMessage restored = Assert.IsType<FutureMessage>(
            JsonSerializer.Deserialize<FutureMessage>(json, ServiceBusMetadataJson.Options));

        Assert.Equal("stored", Assert.IsType<string>(restored.Message[nameof(StoredContract.Value)]));
        Assert.Equal(original.SupportedMessageTypes, restored.SupportedMessageTypes);
        IDictionary<string, object> dictionary = Assert.IsAssignableFrom<IDictionary<string, object>>(restored.Message);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("Other", "value"));
        Assert.False(restored.SupportedMessageTypes is string[]);
    }

    private interface StoredContract
    {
        string Value { get; }
    }

    private interface OtherContract;
}
