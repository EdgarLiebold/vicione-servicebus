using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SignalR.Configuration;

/// <summary>Places an internal backplane consumer on the shared per-node hub endpoint.</summary>
internal sealed class BackplaneConsumerDefinition<TConsumer, THub> :
    ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
    where THub : Hub
{
    /// <summary>Initializes the consumer definition with its hub-specific endpoint.</summary>
    public BackplaneConsumerDefinition(SignalRBackplaneEndpointDefinition<THub> endpointDefinition)
    {
        ArgumentNullException.ThrowIfNull(endpointDefinition);

        EndpointDefinition = endpointDefinition as IEndpointDefinition<TConsumer>
            ?? throw new InvalidOperationException(
                $"The SignalR backplane endpoint does not support consumer '{typeof(TConsumer)}'.");
    }
}
