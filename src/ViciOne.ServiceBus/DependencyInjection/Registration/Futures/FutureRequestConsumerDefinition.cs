using System;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Defines configuration for future request consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
public class FutureRequestConsumerDefinition<TConsumer, TRequest> :
    ConsumerDefinition<TConsumer>,
    IFutureRequestDefinition<TRequest>
    where TRequest : class
    where TConsumer : class, IConsumer<TRequest>
{
    Lazy<Uri> _requestAddress = null!;

    /// <summary>Gets the request address.</summary>
    public Uri RequestAddress =>
        _requestAddress?.Value ??
        throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Future Request Consumer Definition", "unknown", $"The future consumer definition was not configured: {TypeCache<TConsumer>.ShortName}", "Correct the named configuration before starting the host"));

    /// <summary>Configures consumer.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="consumerConfigurator">The consumer configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.ConfigureConsumeTopology = false;

        _requestAddress = new Lazy<Uri>(() => endpointConfigurator.InputAddress);

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator, context);
    }
}
