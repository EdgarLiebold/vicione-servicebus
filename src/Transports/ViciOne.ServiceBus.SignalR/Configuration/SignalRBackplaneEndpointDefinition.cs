using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration;

/// <summary>Co-locates a hub's internal backplane consumers on one isolated temporary endpoint.</summary>
internal sealed class SignalRBackplaneEndpointDefinition<THub> :
    DefaultEndpointDefinition,
    IEndpointDefinition<BroadcastConsumer<THub>>,
    IEndpointDefinition<ConnectionConsumer<THub>>,
    IEndpointDefinition<GroupConsumer<THub>>,
    IEndpointDefinition<GroupCommandConsumer<THub>>,
    IEndpointDefinition<UserConsumer<THub>>,
    IEndpointDefinition<ClientResultConsumer<THub>>,
    IEndpointDefinition<InvocationCancellationConsumer<THub>>
    where THub : Hub
{
    static readonly string EndpointIdentity = CreateEndpointIdentity();

    /// <summary>Initializes a non-durable endpoint whose lifetime matches one application node.</summary>
    public SignalRBackplaneEndpointDefinition()
        : base(isTemporary: true)
    {
    }

    /// <summary>Formats the bounded hub identity through the active transport's endpoint convention.</summary>
    public override string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        return formatter.TemporaryEndpoint(EndpointIdentity);
    }

    static string CreateEndpointIdentity()
    {
        string hubIdentity = $"{typeof(THub).Assembly.GetName().Name}:{typeof(THub).FullName}";
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(hubIdentity));
        return $"signalr-backplane-{Convert.ToHexString(digest.AsSpan(0, 10)).ToLowerInvariant()}";
    }
}
