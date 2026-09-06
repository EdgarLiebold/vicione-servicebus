using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>Defines configuration for group management consumer.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class GroupManagementConsumerDefinition<THub> :
    ConsumerDefinition<GroupManagementConsumer<THub>>
    where THub : Hub
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public GroupManagementConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
