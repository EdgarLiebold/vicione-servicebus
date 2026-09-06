using System;
using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>Defines configuration for hub consumer.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class HubConsumerDefinition<THub> :
    IEndpointDefinition<AllConsumer<THub>>,
    IEndpointDefinition<ConnectionConsumer<THub>>,
    IEndpointDefinition<GroupConsumer<THub>>,
    IEndpointDefinition<GroupManagementConsumer<THub>>,
    IEndpointDefinition<UserConsumer<THub>>
    where THub : Hub
{
    readonly Lazy<string> _hubName = new Lazy<string>(() => typeof(THub).Name.ToLower(CultureInfo.InvariantCulture));

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => true;

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => default;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => default;

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.TemporaryEndpoint($"signalr_{_hubName.Value}");
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
