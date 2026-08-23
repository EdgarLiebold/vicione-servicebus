using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Tests.Serialization;

internal static class SystemTextJsonRoundTrip
{
    private static readonly Uri SourceAddress = new("loopback://localhost/source");
    private static readonly Uri DestinationAddress = new("loopback://localhost/destination");
    private static readonly Uri ResponseAddress = new("loopback://localhost/response");
    private static readonly Uri FaultAddress = new("loopback://localhost/fault");
    private static readonly Guid RequestId = Guid.Parse("f61a42aa-8f43-40e3-9268-ef8d09928fb3");

    internal static T Execute<T>(T message)
        where T : class =>
        ExecuteWithContext(message).Message;

    internal static SystemTextJsonRoundTripResult<T> ExecuteWithContext<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var serializer = new SystemTextJsonMessageSerializer();
        var sendContext = new MessageSendContext<T>(message)
        {
            Serializer = serializer,
            SourceAddress = SourceAddress,
            DestinationAddress = DestinationAddress,
            ResponseAddress = ResponseAddress,
            FaultAddress = FaultAddress,
            RequestId = RequestId,
        };

        MessageBody body = serializer.GetMessageBody(sendContext);
        SerializerContext serializerContext = serializer.Deserialize(body, EmptyHeaders.Instance, DestinationAddress);

        if (!serializerContext.TryGetMessage<T>(out T? roundTripped) || roundTripped is null)
        {
            throw new InvalidDataException(
                $"System.Text.Json did not return a supported message of type '{typeof(T)}'.");
        }

        return new SystemTextJsonRoundTripResult<T>(roundTripped, body.GetBytes());
    }
}

internal sealed record SystemTextJsonRoundTripResult<T>(
    T Message,
    byte[] Bytes)
    where T : class;
