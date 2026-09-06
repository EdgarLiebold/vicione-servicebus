using System;
using ViciOne.ServiceBus.Futures.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Hosts a future beside the request consumer that performs its command.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
/// <typeparam name="TConsumer">The companion request consumer type.</typeparam>
/// <typeparam name="TRequest">The request contract.</typeparam>
/// <typeparam name="TResponse">The successful response contract.</typeparam>
/// <typeparam name="TFault">The terminal future fault contract.</typeparam>
public class RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse, TFault> :
    FutureDefinition<TFuture>,
    IFutureRequestDefinition<TRequest>
    where TFuture : Future<TRequest, TResponse, TFault>
    where TRequest : class
    where TResponse : class
    where TFault : class
    where TConsumer : class, IConsumer<TRequest>
{
    readonly IFutureRequestDefinition<TRequest> _requestDefinition;

    /// <summary>Creates a future definition bound to a compatible companion consumer definition.</summary>
    /// <param name="consumerDefinition">The companion definition that exposes the request endpoint.</param>
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
    {
        ArgumentNullException.ThrowIfNull(consumerDefinition);
        _requestDefinition = consumerDefinition as IFutureRequestDefinition<TRequest>
            ?? throw new ArgumentException(
                $"The consumer definition must derive from {TypeCache<FutureRequestConsumerDefinition<TConsumer, TRequest>>.ShortName}.",
                nameof(consumerDefinition));

        EndpointDefinition = new RequestConsumerFutureEndpointDefinition<TFuture>(this, consumerDefinition);
    }

    /// <summary>Gets the companion consumer endpoint to which the future sends its command.</summary>
    public Uri RequestAddress => _requestDefinition.RequestAddress;

    /// <summary>Applies the standard technical retry and volatile-outbox policies.</summary>
    /// <param name="endpointConfigurator">The future's receive endpoint.</param>
    /// <param name="sagaConfigurator">The future-state saga configurator.</param>
    /// <param name="context">The registration context used to configure the outbox.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(sagaConfigurator);
        ArgumentNullException.ThrowIfNull(context);
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseVolatileOutbox(context);
    }
}


/// <summary>Hosts a future beside the request consumer that performs its command.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
/// <typeparam name="TConsumer">The companion request consumer type.</typeparam>
/// <typeparam name="TRequest">The request contract.</typeparam>
/// <typeparam name="TResponse">The successful response contract.</typeparam>
public class RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse> :
    RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse, Fault<TRequest>>
    where TFuture : Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TConsumer : class, IConsumer<TRequest>
{
    /// <summary>Creates a future definition that uses the conventional request fault contract.</summary>
    /// <param name="consumerDefinition">The companion definition that exposes the request endpoint.</param>
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
        : base(consumerDefinition)
    {
    }
}
