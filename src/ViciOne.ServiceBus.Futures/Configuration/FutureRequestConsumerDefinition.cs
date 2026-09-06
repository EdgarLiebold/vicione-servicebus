using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Captures the endpoint address of a consumer that serves requests from a future.</summary>
/// <typeparam name="TConsumer">The companion request consumer type.</typeparam>
/// <typeparam name="TRequest">The request contract consumed beside the future.</typeparam>
public class FutureRequestConsumerDefinition<TConsumer, TRequest> :
    ConsumerDefinition<TConsumer>,
    IFutureRequestDefinition<TRequest>
    where TRequest : class
    where TConsumer : class, IConsumer<TRequest>
{
    Lazy<Uri>? _requestAddress;

    /// <summary>Gets the configured endpoint address to which the future sends requests.</summary>
    public Uri RequestAddress =>
        _requestAddress?.Value ??
        throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Future Request Consumer Definition", "unknown", $"The future consumer definition was not configured: {TypeCache<TConsumer>.ShortName}", "Correct the named configuration before starting the host"));

    /// <summary>Captures the companion consumer endpoint and disables its consume topology.</summary>
    /// <param name="endpointConfigurator">The companion consumer's receive endpoint.</param>
    /// <param name="consumerConfigurator">The companion consumer configurator.</param>
    /// <param name="context">The registration context that resolves consumer dependencies.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        ArgumentNullException.ThrowIfNull(context);
        endpointConfigurator.ConfigureConsumeTopology = false;

        _requestAddress = new Lazy<Uri>(() => endpointConfigurator.InputAddress);

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator, context);
    }
}
