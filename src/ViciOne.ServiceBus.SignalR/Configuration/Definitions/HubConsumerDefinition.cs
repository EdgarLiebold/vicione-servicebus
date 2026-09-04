using System;
using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Consumers;

namespace ViciOne.ServiceBus.SignalR.Configuration.Definitions;

/// <summary>
/// Provides a hub consumer definition implementation.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public class HubConsumerDefinition<THub> :
    IEndpointDefinition<AllConsumer<THub>>,
    IEndpointDefinition<ConnectionConsumer<THub>>,
    IEndpointDefinition<GroupConsumer<THub>>,
    IEndpointDefinition<GroupManagementConsumer<THub>>,
    IEndpointDefinition<UserConsumer<THub>>
    where THub : Hub
{
    readonly Lazy<string> _hubName = new Lazy<string>(() => typeof(THub).Name.ToLower(CultureInfo.InvariantCulture));

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => true;

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount => default;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => default;

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.TemporaryEndpoint($"signalr_{_hubName.Value}");
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
