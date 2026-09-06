using System;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Defines configuration for request consumer future.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumerDefinition">The consumer definition.</param>
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
    {
        if (consumerDefinition is IFutureRequestDefinition<TRequest> requestDefinition)
            _requestDefinition = requestDefinition;

        EndpointDefinition = new RequestConsumerFutureEndpointDefinition<TFuture>(this, consumerDefinition);
    }

    /// <summary>Gets the request address.</summary>
    public Uri RequestAddress =>
        _requestDefinition?.RequestAddress ??
        throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Request Consumer Future Definition", "unknown", $"The consumer definition was not a FutureConsumerDefinition: {TypeCache<TConsumer>.ShortName}", "Correct the named configuration before starting the host"));

    /// <summary>Configures saga.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="sagaConfigurator">The saga configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseVolatileOutbox(context);
    }
}


/// <summary>Defines configuration for request consumer future.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse> :
    RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse, Fault<TRequest>>
    where TFuture : Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TConsumer : class, IConsumer<TRequest>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumerDefinition">The consumer definition.</param>
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
        : base(consumerDefinition)
    {
    }
}
