// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Represents a monitor for bus activity, allowing awaiting an inactive bus state.
    /// </summary>
    public interface IBusActivityMonitor
    {
        Task AwaitBusInactivity();

        Task<bool> AwaitBusInactivity(TimeSpan timeout);

        Task AwaitBusInactivity(CancellationToken cancellationToken);
    }
}
