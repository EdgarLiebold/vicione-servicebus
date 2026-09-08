using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

internal static class MessagePackRoundTrip
{
    private static readonly Uri SourceAddress = new("loopback://localhost/source");
    private static readonly Uri DestinationAddress = new("loopback://localhost/destination");
    private static readonly Uri ResponseAddress = new("loopback://localhost/response");
    private static readonly Uri FaultAddress = new("loopback://localhost/fault");

    internal static T Execute<T>(T message)
        where T : class =>
        ExecuteWithContext(message).Message;

    internal static RoundTripResult<T> ExecuteWithContext<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var serializer = new MessagePackMessageSerializer();
        var requestId = Guid.NewGuid();
        var sendContext = new MessageSendContext<T>(message)
        {
            Serializer = serializer,
            SourceAddress = SourceAddress,
            DestinationAddress = DestinationAddress,
            ResponseAddress = ResponseAddress,
            FaultAddress = FaultAddress,
            RequestId = requestId,
        };
        sendContext.Headers.Set("test-header", "preserved");

        var body = serializer.GetMessageBody(sendContext);
        var serializerContext = serializer.Deserialize(body, EmptyHeaders.Instance, DestinationAddress);

        if (!serializerContext.TryGetMessage<T>(out var roundTripped) || roundTripped is null)
        {
            throw new InvalidDataException(
                $"MessagePack did not return a supported message of type '{typeof(T)}'.");
        }

        return new RoundTripResult<T>(roundTripped, serializerContext, body.GetBytes(), requestId);
    }
}

internal sealed record RoundTripResult<T>(
    T Message,
    SerializerContext Context,
    byte[] Bytes,
    Guid RequestId)
    where T : class;
