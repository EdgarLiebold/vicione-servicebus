using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Coordinates a durable request and response exchange with a companion consumer endpoint.</summary>
/// <typeparam name="TRequest">The command and companion-consumer request contract.</typeparam>
/// <typeparam name="TResponse">The successful future result contract.</typeparam>
public class RequestConsumerFuture<TRequest, TResponse> :
    Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>Configures the future to send its command to the endpoint exposed by its definition.</summary>
    /// <param name="definition">The future definition that exposes the companion request endpoint.</param>
    public RequestConsumerFuture(IFutureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition is not IFutureRequestDefinition<TRequest> settings)
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(definition.GetType())} does not expose a request address for {TypeCache<TRequest>.ShortName}.",
                nameof(definition));
        }

        SendRequest<TRequest>(x => x.RequestAddress = settings.RequestAddress)
            .OnResponseReceived<TResponse>(x => x.SetResultFromInput());
    }
}
