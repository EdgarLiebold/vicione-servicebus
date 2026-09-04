using System;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a future request consumer definition implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class FutureRequestConsumerDefinition<TConsumer, TRequest> :
    ConsumerDefinition<TConsumer>,
    IFutureRequestDefinition<TRequest>
    where TRequest : class
    where TConsumer : class, IConsumer<TRequest>
{
    Lazy<Uri> _requestAddress = null!;

    /// <summary>
    /// Gets the request address value.
    /// </summary>
    public Uri RequestAddress =>
        _requestAddress?.Value ??
        throw new ConfigurationException($"The future consumer definition was not configured: {TypeCache<TConsumer>.ShortName}");

    /// <summary>
    /// Configures consumer.
    /// </summary>
    /// <param name="endpointConfigurator">The endpoint configurator value.</param>
    /// <param name="consumerConfigurator">The consumer configurator value.</param>
    /// <param name="context">The operation context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.ConfigureConsumeTopology = false;

        _requestAddress = new Lazy<Uri>(() => endpointConfigurator.InputAddress);

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator, context);
    }
}
