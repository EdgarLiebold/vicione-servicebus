using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.SqlTransport.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Serialization;

public sealed class SqlMessageBodyStorageTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "provider-storage-rejects-body-metadata-mutation")]
    public void Create_RejectsBodyCallbacksThatChangeSqlMessageMetadata(bool binary, bool changeContentType)
    {
        Guid originalId = Guid.Parse("5a24bfa2-c8c9-4234-9cb9-79f5be174262");
        Guid laterId = Guid.Parse("35863cf5-3295-411a-84ef-3bc0ee454752");
        string originalContentType = binary ? "application/octet-stream" : "application/json";
        var context = new SqlMessageSendContext<object>(new object(), CancellationToken.None)
        {
            MessageId = originalId,
            Serializer = new MutatingSqlSerializer(originalContentType, binary, sendContext =>
            {
                if (changeContentType)
                    sendContext.ContentType = new ContentType("application/vnd.vicione.changed");
                else
                    sendContext.MessageId = laterId;
            }),
        };

        MessageException failure = Assert.Throws<MessageException>(() => SqlMessageBodyStorage.Create(context));

        Assert.Contains(changeContentType ? "ContentType changed" : "MessageId changed", failure.Message,
            StringComparison.Ordinal);
        Assert.Equal(originalId, context.MessageId);
        Assert.Equal(originalContentType, context.ContentType?.ToString());
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.vicione.servicebus+json")]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "json-bodies-remain-queryable-text")]
    public void Create_PreservesJsonAsQueryableText(string mediaType)
    {
        const string json = "{\"message\":42}";

        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(
            new StringMessageBody(json),
            new ContentType(mediaType));

        Assert.Equal(json, storage.Text);
        Assert.Null(storage.Binary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "non-json-bodies-use-lossless-binary-storage")]
    public void Create_PreservesNonJsonBytesWithoutUsingATextCarrier()
    {
        byte[] expected = [0xc1, 0x00, 0xff, 0x2a];

        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(
            new BinaryMessageBody(expected),
            new ContentType("application/vnd.vicione.servicebus+msgpack"));

        Assert.Null(storage.Text);
        Assert.Equal(expected, storage.Binary);
        Assert.NotSame(expected, storage.Binary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "missing-body-and-content-type-boundaries")]
    public void Create_RejectsAMissingBodyAndUsesBinaryStorageWithoutAContentType()
    {
        byte[] expected = [0x10, 0x20, 0x30];

        Assert.Equal(
            "body",
            Assert.Throws<ArgumentNullException>(() => SqlMessageBodyStorage.Create(null!, null)).ParamName);
        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(new BinaryMessageBody(expected), null);

        Assert.Null(storage.Text);
        Assert.Equal(expected, storage.Binary);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.vicione.servicebus+json")]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "json-media-type-without-text-carrier-remains-binary")]
    public void Create_DoesNotDecodeAnOpaqueBodySolelyBecauseItsMediaTypeIsJson(string mediaType)
    {
        byte[] expected = "{\"message\":42}"u8.ToArray();

        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(
            new BinaryMessageBody(expected),
            new ContentType(mediaType));

        Assert.Null(storage.Text);
        Assert.Equal(expected, storage.Binary);
        Assert.NotSame(expected, storage.Binary);
    }

    private sealed class MutatingSqlSerializer(string mediaType, bool binary, Action<SendContext> mutate) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new(mediaType);

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new MutatingSqlBody(binary, () => mutate(context));
    }

    private sealed class MutatingSqlBody(bool binary, Action mutate) : MessageBody
    {
        private static readonly byte[] BodyBytes = "{}"u8.ToArray();

        public long Length => BodyBytes.Length;

        public byte[] ToArray()
        {
            mutate();
            return (byte[])BodyBytes.Clone();
        }

        public Stream OpenReadStream() => new MemoryStream(ToArray(), writable: false);

        public bool TryGetTransportText([NotNullWhen(true)] out string? text)
        {
            if (binary)
            {
                text = null;
                return false;
            }

            mutate();
            text = "{}";
            return true;
        }
    }
}
