// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Published when a message fails to deserialize at the endpoint
    /// </summary>
    public interface ReceiveFault :
        Fault
    {
        /// <summary>
        /// The specified content type of the message by the transport
        /// </summary>
        string ContentType { get; }
    }
}
