using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Connects a message handler to a pipe
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class MessageObserverConnector<TMessage> :
    IObserverConnector<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Connects observer.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="observer">The observer value.</param>
    /// <param name="filters">The filters value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Connects request observer.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="requestId">The request id value.</param>
    /// <param name="observer">The observer value.</param>
    /// <param name="filters">The filters value.</param>
    /// <returns>The result of the operation.</returns>
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
