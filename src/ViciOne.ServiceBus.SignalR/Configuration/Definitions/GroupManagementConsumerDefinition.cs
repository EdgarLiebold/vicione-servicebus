using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

public class GroupManagementConsumerDefinition<THub> :
    ConsumerDefinition<GroupManagementConsumer<THub>>
    where THub : Hub
{
    public GroupManagementConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
