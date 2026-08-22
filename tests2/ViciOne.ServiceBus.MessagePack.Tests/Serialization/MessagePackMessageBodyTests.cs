using MessagePack;
using MessagePack.Resolvers;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackMessageBodyTests
{
    private static readonly MessagePackSerializerOptions ExternalOracleOptions =
        MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);

    [Theory]
    [InlineData(FirstAccessor.Length)]
    [InlineData(FirstAccessor.Bytes)]
    [InlineData(FirstAccessor.String)]
    [InlineData(FirstAccessor.Stream)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-BODY", "accessor-order-and-read-only-stream")]
    public void EveryAccessorOrder_DescribesTheSameReadOnlyBody(FirstAccessor firstAccessor)
    {
        var source = new BodyMessage { Id = 27, Text = "Grüße" };
        var body = new MessagePackMessageBody<BodyMessage>(source);

        ReadFirst(body, firstAccessor);

        byte[] bytes = body.GetBytes();
        string text = body.GetString();
        using var stream = body.GetStream();
        using var streamed = new MemoryStream();
        stream.CopyTo(streamed);
        var expected = MessagePackSerializer.Serialize(
            source,
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);
        var restored = MessagePackSerializer.Deserialize<BodyMessage>(
            bytes,
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, bytes);
        Assert.Equal(bytes.LongLength, body.Length);
        Assert.Equal(Convert.ToBase64String(bytes), text);
        Assert.Equal(bytes, streamed.ToArray());
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0xFF));
        Assert.Equal(expected, body.GetBytes());
        Assert.Equal(27, restored.Id);
        Assert.Equal("Grüße", restored.Text);
    }

    private static void ReadFirst(MessageBody body, FirstAccessor firstAccessor)
    {
        switch (firstAccessor)
        {
            case FirstAccessor.Length:
                _ = body.Length;
                break;
            case FirstAccessor.Bytes:
                _ = body.GetBytes();
                break;
            case FirstAccessor.String:
                _ = body.GetString();
                break;
            case FirstAccessor.Stream:
                body.GetStream().Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(firstAccessor), firstAccessor, null);
        }
    }

    public enum FirstAccessor
    {
        Length,
        Bytes,
        String,
        Stream,
    }

    public sealed class BodyMessage
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;
    }
}
