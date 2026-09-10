using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects consume observers to normal or request-correlated message pipes.</summary>
/// <typeparam name="TMessage">The message contract reported to the observer.</typeparam>
public sealed class MessageObserverConnector<TMessage> :
    IObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects an observer for every message of the configured contract.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="observer">The observer notified after the preceding filters.</param>
    /// <param name="filters">Optional filters that run before the observer.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectObserver(IConsumePipeConnector consumePipe, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        IPipe<ConsumeContext<TMessage>> pipe = BuildPipe(observer, filters);

        return consumePipe.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects an observer for messages with the specified request identifier.</summary>
    /// <param name="consumePipe">The request pipe that will dispatch the matching request.</param>
    /// <param name="requestId">The request identifier that selects messages for the observer.</param>
    /// <param name="observer">The observer notified after the preceding filters.</param>
    /// <param name="filters">Optional filters that run before the observer.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestObserver(IRequestPipeConnector consumePipe, Guid requestId, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        IPipe<ConsumeContext<TMessage>> pipe = BuildPipe(observer, filters);

        return consumePipe.ConnectRequestPipe(requestId, pipe);
    }

    static IPipe<ConsumeContext<TMessage>> BuildPipe(
        IObserver<ConsumeContext<TMessage>> observer,
        IFilter<ConsumeContext<TMessage>>[] filters)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(filters);

        return Pipe.New<ConsumeContext<TMessage>>(configurator =>
        {
            foreach (IFilter<ConsumeContext<TMessage>> filter in filters)
            {
                ArgumentNullException.ThrowIfNull(filter);
                configurator.UseFilter(filter);
            }

            configurator.AddPipeSpecification(new ObserverPipeSpecification<TMessage>(observer));
        });
    }
}
