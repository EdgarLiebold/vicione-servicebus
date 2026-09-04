using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

public class GroupConsumerDefinition<THub> :
    ConsumerDefinition<GroupConsumer<THub>>
    where THub : Hub
{
    public GroupConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
