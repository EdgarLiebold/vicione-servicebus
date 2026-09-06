using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Used to observe the events signaled by a receive endpoint.</summary>
public interface IReceiveTransportObserver
{
    /// <summary>Called when the receive endpoint is ready to receive messages.</summary>
    /// <param name="ready">The ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReadyAsync(ReceiveTransportReady ready);

    /// <summary>Called when the receive endpoint has completed.</summary>
    /// <param name="completed">The completed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompletedAsync(ReceiveTransportCompleted completed);

    /// <summary>Called when the receive endpoint faults.</summary>
    /// <param name="faulted">The faulted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync(ReceiveTransportFaulted faulted);
}
