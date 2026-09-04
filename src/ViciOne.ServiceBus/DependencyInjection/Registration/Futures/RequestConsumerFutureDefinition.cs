using System;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

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

    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
    {
        if (consumerDefinition is IFutureRequestDefinition<TRequest> requestDefinition)
            _requestDefinition = requestDefinition;

        EndpointDefinition = new RequestConsumerFutureEndpointDefinition<TFuture>(this, consumerDefinition);
    }

    public Uri RequestAddress =>
        _requestDefinition?.RequestAddress ??
        throw new ConfigurationException($"The consumer definition was not a FutureConsumerDefinition: {TypeCache<TConsumer>.ShortName}");

    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalMessageRetry();
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}


public class RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse> :
    RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse, Fault<TRequest>>
    where TFuture : Future<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TConsumer : class, IConsumer<TRequest>
{
    public RequestConsumerFutureDefinition(IConsumerDefinition<TConsumer> consumerDefinition)
        : base(consumerDefinition)
    {
    }
}
