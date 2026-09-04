using Apache.NMS;

namespace ViciOne.ServiceBus;

public interface ActiveMqMessageContext
{
    IMessage TransportMessage { get; }

    IPrimitiveMap Properties { get; }

    string? GroupId { get; }
    int GroupSequence { get; }
}
