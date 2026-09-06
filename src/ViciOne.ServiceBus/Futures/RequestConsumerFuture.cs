using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Coordinates the future result for request consumer.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class RequestConsumerFuture<TRequest, TResponse> :
    Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="definition">The definition.</param>
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
