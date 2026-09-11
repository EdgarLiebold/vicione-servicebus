using ViciOne.ServiceBus.MessageData.Serialization;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataValueTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "stored-binary-snapshot-isolation")]
    public async Task StoredBinaryValue_CapturesAndReturnsIndependentSnapshotsAsync()
    {
        byte[] source = [3, 4, 5];
        var value = new StoredMessageData<byte[]>(new Uri("urn:stored:binary"), source);

        source[0] = 9;
        byte[] first = Assert.IsType<byte[]>(await value.Value);
        first[1] = 8;
        byte[] second = Assert.IsType<byte[]>(await value.Value);

        Assert.Equal([3, 4, 5], second);
        Assert.NotSame(source, first);
        Assert.NotSame(first, second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "value-constructor-boundaries")]
    public void ValueImplementations_RejectMissingRequiredStateAtTheOwningBoundary()
    {
        var address = new Uri("urn:message-data:value");
        var inlineText = new StringInlineMessageData("value");

        Assert.Equal("address", Assert.Throws<ArgumentNullException>(() =>
            new DeserializedMessageData<string>(null!)).ParamName);
        Assert.Equal("address", Assert.Throws<ArgumentNullException>(() =>
            new StoredMessageData<string>(null!, "value")).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
            new StoredMessageData<string>(address, null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
            new PutMessageData<string>(null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
            new StringInlineMessageData(null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
            new BytesInlineMessageData(null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
            new InlineMessageData<string>(null, null!, inlineText)).ParamName);
        Assert.Equal("messageData", Assert.Throws<ArgumentNullException>(() =>
            new InlineMessageData<string>(null, "value", null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "inline-reference-mutual-exclusion")]
    public void InlineValues_WriteExactlyOneIndependentReferenceRepresentation()
    {
        var textReference = new Reference { Data = [9] };
        var bytesReference = new Reference { Text = "old" };
        var text = new StringInlineMessageData("current");
        var bytes = new BytesInlineMessageData([1, 2, 3]);

        text.Set(textReference);
        bytes.Set(bytesReference);
        bytesReference.Data![0] = 8;
        var secondBytesReference = new Reference();
        bytes.Set(secondBytesReference);

        Assert.Equal("current", textReference.Text);
        Assert.Null(textReference.Data);
        Assert.Null(bytesReference.Text);
        Assert.Equal([1, 2, 3], secondBytesReference.Data);
        Assert.Equal("reference", Assert.Throws<ArgumentNullException>(() => text.Set(null!)).ParamName);
        Assert.Equal("reference", Assert.Throws<ArgumentNullException>(() => bytes.Set(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-CONVERSION", "empty-and-unresolved-access-failures")]
    public async Task EmptyAndUnresolvedValues_FailWithExplicitMessageDataErrorsAsync()
    {
        MessageData<string> empty = EmptyMessageData<string>.Instance;
        var unresolved = new DeserializedMessageData<string>(new Uri("urn:message-data:unresolved"));

        Assert.False(empty.HasValue);
        Assert.Throws<MessageDataException>(() => _ = empty.Address);
        await Assert.ThrowsAsync<MessageDataException>(() => empty.Value);
        Assert.True(unresolved.HasValue);
        Assert.Equal(new Uri("urn:message-data:unresolved"), unresolved.Address);
        MessageDataException exception = Assert.Throws<MessageDataException>(() =>
        {
            _ = unresolved.Value;
        });
        Assert.Contains("was not loaded", exception.Message, StringComparison.Ordinal);
    }

    private sealed class Reference : IMessageDataReference
    {
        public string? Text { get; set; }

        public byte[]? Data { get; set; }
    }
}
