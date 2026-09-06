using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>Defines configuration for user consumer.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class UserConsumerDefinition<THub> :
    ConsumerDefinition<UserConsumer<THub>>
    where THub : Hub
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public UserConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
