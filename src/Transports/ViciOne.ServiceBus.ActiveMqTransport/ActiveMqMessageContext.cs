// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Apache.NMS;


    public interface ActiveMqMessageContext
    {
        IMessage TransportMessage { get; }

        IPrimitiveMap Properties { get; }

        string GroupId { get; }
        int GroupSequence { get; }
    }
}
