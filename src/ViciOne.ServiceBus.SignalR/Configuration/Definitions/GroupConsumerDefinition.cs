using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>
/// Provides a group consumer definition implementation.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public class GroupConsumerDefinition<THub> :
    ConsumerDefinition<GroupConsumer<THub>>
    where THub : Hub
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    public GroupConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
