using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class ReliableSchedulerEndpointConfiguration<TBus>(
    ReliableSchedulerSelection<TBus> selection) :
    IConfigureReceiveEndpoint
    where TBus : class, IBus
{
    readonly ReliableSchedulerSelection<TBus> _selection = selection
        ?? throw new ArgumentNullException(nameof(selection));

    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        switch (_selection.Kind)
        {
            case ReliableSchedulerAdapterKind.Store:
            case ReliableSchedulerAdapterKind.Transport:
                // Store-backed scheduling persists DueAt; transport-backed scheduling applies native delay.
                configurator.AddPrePipeSpecification(new DelayedMessageSchedulerSpecification());
                break;

            case ReliableSchedulerAdapterKind.Endpoint:
                configurator.AddPrePipeSpecification(new MessageSchedulerPipeSpecification(
                    _selection.EndpointAddress
                    ?? throw new ConfigurationException(
                        global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                            "Scheduling",
                            "unknown",
                            "The selected endpoint scheduler has no address.",
                            "Choose a scheduler adapter with an absolute endpoint address"))));
                break;

            default:
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Scheduling",
                        "unknown",
                        $"The scheduler adapter value '{_selection.Kind}' is not supported.",
                        "Choose one of the declared reliable-messaging scheduler adapters"));
        }
    }
}
