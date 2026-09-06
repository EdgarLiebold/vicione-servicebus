namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines settings for endpoint.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IEndpointSettings<TConsumer>
    where TConsumer : class
{
    /// <summary>Gets the name.</summary>
    string? Name { get; }

    /// <summary>Gets a value indicating whether temporary.</summary>
    bool IsTemporary { get; }

    /// <summary>Gets the prefetch count.</summary>
    int? PrefetchCount { get; }

    /// <summary>Gets the concurrent message limit.</summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>Gets the configure consume topology.</summary>
    bool ConfigureConsumeTopology { get; }

    /// <summary>Gets the instance id.</summary>
    string? InstanceId { get; }

    /// <summary>Configures endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void ConfigureEndpoint<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator;
}
