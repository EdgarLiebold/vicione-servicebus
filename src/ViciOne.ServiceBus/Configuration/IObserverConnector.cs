using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a consume observer to normal or request-correlated message pipes.</summary>
/// <typeparam name="TMessage">The observed message contract.</typeparam>
public interface IObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects an observer for every message of the configured contract.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="observer">The observer notified after the preceding filters.</param>
    /// <param name="filters">Optional filters that run before the observer.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectObserver(IConsumePipeConnector consumePipe, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters);

    /// <summary>Connects an observer for messages with the specified request identifier.</summary>
    /// <param name="consumePipe">The request pipe that will dispatch the matching request.</param>
    /// <param name="requestId">The request identifier that selects messages for the observer.</param>
    /// <param name="observer">The observer notified after the preceding filters.</param>
    /// <param name="filters">Optional filters that run before the observer.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectRequestObserver(IRequestPipeConnector consumePipe, Guid requestId, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters);
}
