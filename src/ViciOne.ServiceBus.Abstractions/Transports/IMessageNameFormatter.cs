// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;


    /// <summary>
    /// Used to format a message type into a MessageName, which can be used as a valid
    /// queue name on the transport
    /// </summary>
    public interface IMessageNameFormatter
    {
        string GetMessageName(Type type);
    }
}
