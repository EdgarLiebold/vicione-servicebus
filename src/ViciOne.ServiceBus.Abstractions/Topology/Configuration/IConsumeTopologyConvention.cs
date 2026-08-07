// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// A convention that is applies to a message type on Consume, if applicable to
    /// the message type.
    /// </summary>
    public interface IConsumeTopologyConvention :
        IMessageConsumeTopologyConvention
    {
    }
}
