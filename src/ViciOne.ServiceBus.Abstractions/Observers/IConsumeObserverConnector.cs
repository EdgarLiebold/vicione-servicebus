// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Supports connection of a consume observer
    /// </summary>
    public interface IConsumeObserverConnector
    {
        ConnectHandle ConnectConsumeObserver(IConsumeObserver observer);
    }
}
