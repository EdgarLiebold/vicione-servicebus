// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    public interface IInactivityObservationSource
    {
        /// <summary>
        /// True if the inactivity source is currently inactive
        /// </summary>
        bool IsInactive { get; }

        ConnectHandle ConnectInactivityObserver(IInactivityObserver observer);
    }
}
