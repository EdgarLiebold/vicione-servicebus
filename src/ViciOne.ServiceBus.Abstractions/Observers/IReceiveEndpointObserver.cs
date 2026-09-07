using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Observes readiness, shutdown, completion, and failure transitions of a receive endpoint.</summary>
public interface IReceiveEndpointObserver
{
    /// <summary>Observes that the receive endpoint is ready to accept messages.</summary>
    /// <param name="ready">The endpoint readiness event.</param>
    /// <returns>A task that completes after the observation has been processed.</returns>
    Task ReadyAsync(ReceiveEndpointReady ready);

    /// <summary>Observes that the receive endpoint is about to stop.</summary>
    /// <param name="stopping">The endpoint stopping event.</param>
    /// <returns>A task that completes after the observation has been processed.</returns>
    Task StoppingAsync(ReceiveEndpointStopping stopping);

    /// <summary>Observes that the receive endpoint's transport has completed.</summary>
    /// <param name="completed">The endpoint completion event.</param>
    /// <returns>A task that completes after the observation has been processed.</returns>
    Task CompletedAsync(ReceiveEndpointCompleted completed);

    /// <summary>Observes a recoverable or terminal receive-endpoint failure.</summary>
    /// <param name="faulted">The endpoint failure event.</param>
    /// <returns>A task that completes after the observation has been processed.</returns>
    Task FaultedAsync(ReceiveEndpointFaulted faulted);
}
