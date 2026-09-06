using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a message handler to the ConsumePipe.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connect a message handler for all messages of type T.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="filters">The filters.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectObserver(IConsumePipeConnector consumePipe, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters);

    /// <summary>Connect a message handler for messages with the specified RequestId.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="filters">The filters.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRequestObserver(IRequestPipeConnector consumePipe, Guid requestId, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters);
}
