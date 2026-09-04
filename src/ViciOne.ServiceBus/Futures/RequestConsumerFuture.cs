using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a request consumer future implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestConsumerFuture<TRequest, TResponse> :
    Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    public RequestConsumerFuture(IFutureDefinition definition)
    {
        if (!(definition is IFutureRequestDefinition<TRequest> settings))
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(definition.GetType())} does not implement {TypeCache<IFutureRequestDefinition<TRequest>>.ShortName}");
        }

        SendRequest<TRequest>(x => x.RequestAddress = settings.RequestAddress)
            .OnResponseReceived<TResponse>(x => x.SetCompleted());
    }
}
