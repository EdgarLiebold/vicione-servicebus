using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>Defines configuration for connection consumer.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class ConnectionConsumerDefinition<THub> :
    ConsumerDefinition<ConnectionConsumer<THub>>
    where THub : Hub
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public ConnectionConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
