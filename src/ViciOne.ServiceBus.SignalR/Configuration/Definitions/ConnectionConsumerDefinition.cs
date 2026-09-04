using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

public class ConnectionConsumerDefinition<THub> :
    ConsumerDefinition<ConnectionConsumer<THub>>
    where THub : Hub
{
    public ConnectionConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
