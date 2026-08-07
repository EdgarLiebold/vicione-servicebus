// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// The context associated with stopping an agent
    /// </summary>
    public interface StopContext :
        PipeContext
    {
        /// <summary>
        /// The reason for stopping
        /// </summary>
        string Reason { get; }
    }
}
