using ViciOne.ServiceBus.Futures;
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

    private interface StoredContract
    {
        string Value { get; }
    }

    private interface OtherContract;
}
