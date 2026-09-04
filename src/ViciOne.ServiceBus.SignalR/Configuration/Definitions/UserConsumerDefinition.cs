using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>
/// Provides an user consumer definition implementation.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public class UserConsumerDefinition<THub> :
    ConsumerDefinition<UserConsumer<THub>>
    where THub : Hub
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointDefinition">The endpoint definition value.</param>
    public UserConsumerDefinition(HubConsumerDefinition<THub> endpointDefinition)
    {
        EndpointDefinition = endpointDefinition;
    }
}
