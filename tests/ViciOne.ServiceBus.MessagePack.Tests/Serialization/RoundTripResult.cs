using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

internal sealed record RoundTripResult<T>(
    T Message,
    SerializerContext Context,
    byte[] Bytes,
    Guid RequestId)
    where T : class;
