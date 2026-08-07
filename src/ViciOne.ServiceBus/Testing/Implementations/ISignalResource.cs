// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    /// <summary>
    /// Represents a resource which may be signaled.
    /// </summary>
    public interface ISignalResource
    {
        void Signal();
    }
}
