using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>Defines configuration for all consumer.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class AllConsumerDefinition<THub> :
    ConsumerDefinition<AllConsumer<THub>>
    where THub : Hub
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public AllConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
