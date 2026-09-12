using MessagePack;
using MessagePack.Resolvers;
using ViciOne.ServiceBus.MessagePack.Serialization;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "messagepack-concrete-type-set")]
    public void EveryConcreteMessagePackMessageBody_IsInTheExplicitContractSet()
    {
        string expected = IdentityOf(typeof(MessagePackMessageBody<>));
        string[] actual =
        [..
            typeof(MessagePackMessageBody<>).Assembly.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract && typeof(MessageBody).IsAssignableFrom(type))
            .Select(Normalize)
            .Distinct()
            .Select(IdentityOf)
            .Order(StringComparer.Ordinal),
        ];

        Assert.Equal([expected], actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-BODY", "constructor-boundaries")]
    public void Constructors_RejectMissingMessagesAtTheirOwningBoundaries()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackMessageBody<BodyMessage>((SendContext<BodyMessage>)null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackMessageBody<BodyMessage>((BodyMessage)null!)).ParamName);
    }

    [Theory]
    [InlineData(FirstAccessor.Length)]
    [InlineData(FirstAccessor.Bytes)]
    [InlineData(FirstAccessor.TransportText)]
    [InlineData(FirstAccessor.ReadStream)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-BODY", "owned-snapshot-single-serialization-and-read-only-accessors")]
    public void EveryAccessorOrder_DescribesTheSameOwnedBody(FirstAccessor firstAccessor)
    {
        var oracle = new BodyMessage { Id = 27, Text = "Grüße" };
        byte[] expected = MessagePackSerializer.Serialize(
            oracle,
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);
        var source = new BodyMessage { Id = 27, Text = "Grüße" };
        var body = new MessagePackMessageBody<BodyMessage>(source);
        Assert.Equal(1, source.GetSerializationCount());
        source.Id = 99;
        source.Text = "changed";

        ReadFirst(body, firstAccessor);

        byte[] bytes = body.ToArray();
        string text = body.GetRequiredTransportText();
        using var stream = body.OpenReadStream();
        using var independent = body.OpenReadStream();
        using var streamed = new MemoryStream();
        stream.CopyTo(streamed);
        var restored = MessagePackSerializer.Deserialize<BodyMessage>(
            bytes,
            ExternalOracleOptions,
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, bytes);
        Assert.Equal(bytes.LongLength, body.Length);
        Assert.Equal(Convert.ToBase64String(bytes), text);
        Assert.Equal(bytes, streamed.ToArray());
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => { stream.WriteByte(0xFF); });
        MemoryStream memoryStream = Assert.IsType<MemoryStream>(stream);
        Assert.False(memoryStream.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(memoryStream.GetBuffer);
        Assert.NotSame(stream, independent);
        Assert.Equal(0, independent.Position);
        bytes.AsSpan().Fill(0x00);
        Assert.Equal(expected, body.ToArray());
        Assert.Equal(27, restored.Id);
        Assert.Equal("Grüße", restored.Text);

        Parallel.For(0, 128, _ =>
        {
            Assert.Equal(expected.LongLength, body.Length);
            Assert.Equal(expected, body.ToArray());
            Assert.Equal(expected, Read(body.OpenReadStream()));
            Assert.Equal(Convert.ToBase64String(expected), body.GetRequiredTransportText());
        });
        Assert.Equal(1, source.GetSerializationCount());
    }

    private static void ReadFirst(MessageBody body, FirstAccessor firstAccessor)
    {
        switch (firstAccessor)
        {
            case FirstAccessor.Length:
                _ = body.Length;
                break;
            case FirstAccessor.Bytes:
                _ = body.ToArray();
                break;
            case FirstAccessor.TransportText:
                _ = body.GetRequiredTransportText();
                break;
            case FirstAccessor.ReadStream:
                body.OpenReadStream().Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(firstAccessor), firstAccessor, null);
        }
    }

    private static Type Normalize(Type type) =>
        type.IsGenericType && !type.IsGenericTypeDefinition
            ? type.GetGenericTypeDefinition()
            : type;

    private static string IdentityOf(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"Type '{type.Name}' has no full name.");

    public enum FirstAccessor
    {
        Length,
        Bytes,
        TransportText,
        ReadStream,
    }

    public sealed class BodyMessage : IMessagePackSerializationCallbackReceiver
    {
        int _serializationCount;

        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public int GetSerializationCount() => Volatile.Read(ref _serializationCount);

        public void OnBeforeSerialize() => Interlocked.Increment(ref _serializationCount);

        public void OnAfterDeserialize()
        {
        }
    }

    static byte[] Read(Stream stream)
    {
        using (stream)
        using (var destination = new MemoryStream())
        {
            stream.CopyTo(destination);
            return destination.ToArray();
        }
    }
}
