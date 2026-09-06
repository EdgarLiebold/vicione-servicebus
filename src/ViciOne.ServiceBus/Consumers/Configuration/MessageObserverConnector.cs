using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a message handler to a pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageObserverConnector<TMessage> :
    IObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>Connects observer.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="filters">The filters.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectObserver(IConsumePipeConnector consumePipe, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters)
    {
        IPipe<ConsumeContext<TMessage>> pipe = Pipe.New<ConsumeContext<TMessage>>(x =>
        {
            foreach (IFilter<ConsumeContext<TMessage>> filter in filters)
                x.UseFilter(filter);

            x.AddPipeSpecification(new ObserverPipeSpecification<TMessage>(observer));
        });

        return consumePipe.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects request observer.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <param name="filters">The filters.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestObserver(IRequestPipeConnector consumePipe, Guid requestId, IObserver<ConsumeContext<TMessage>> observer,
        params IFilter<ConsumeContext<TMessage>>[] filters)
    {
        IPipe<ConsumeContext<TMessage>> pipe = Pipe.New<ConsumeContext<TMessage>>(x =>
        {
            foreach (IFilter<ConsumeContext<TMessage>> filter in filters)
                x.UseFilter(filter);

            x.AddPipeSpecification(new ObserverPipeSpecification<TMessage>(observer));
        });

        return consumePipe.ConnectRequestPipe(requestId, pipe);
    }
}
