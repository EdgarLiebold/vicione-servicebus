using System;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a request consumer future definition implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
public class RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse, TFault> :
    FutureDefinition<TFuture>,
    IFutureRequestDefinition<TRequest>
    where TFuture : Future<TRequest, TResponse, TFault>
    where TRequest : class
    where TResponse : class
    where TFault : class
    where TConsumer : class, IConsumer<TRequest>
{
    readonly IFutureRequestDefinition<TRequest> _requestDefinition = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumerDefinition">The consumer definition value.</param>
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
    {
        if (consumerDefinition is IFutureRequestDefinition<TRequest> requestDefinition)
            _requestDefinition = requestDefinition;

        EndpointDefinition = new RequestConsumerFutureEndpointDefinition<TFuture>(this, consumerDefinition);
    }

    /// <summary>
    /// Gets the request address value.
    /// </summary>
    public Uri RequestAddress =>
        _requestDefinition?.RequestAddress ??
        throw new ConfigurationException($"The consumer definition was not a FutureConsumerDefinition: {TypeCache<TConsumer>.ShortName}");

    /// <summary>
    /// Configures saga.
    /// </summary>
    /// <param name="endpointConfigurator">The endpoint configurator value.</param>
    /// <param name="sagaConfigurator">The saga configurator value.</param>
    /// <param name="context">The operation context.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}


/// <summary>
/// Provides a request consumer future definition implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse> :
    RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse, Fault<TRequest>>
    where TFuture : Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TConsumer : class, IConsumer<TRequest>
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumerDefinition">The consumer definition value.</param>
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
        : base(consumerDefinition)
    {
    }
}
