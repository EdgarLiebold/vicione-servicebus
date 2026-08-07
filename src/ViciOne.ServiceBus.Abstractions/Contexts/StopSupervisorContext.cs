// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface StopSupervisorContext :
        StopContext
    {
        /// <summary>
        /// The agents available when the Stop was initiated
        /// </summary>
        IAgent[] Agents { get; }
    }
}
