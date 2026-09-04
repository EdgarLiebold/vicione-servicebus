namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for endpoint settings.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public interface IEndpointSettings<TConsumer>
    where TConsumer : class
{
    /// <summary>
    /// Gets the name value.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    bool IsTemporary { get; }

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    int? PrefetchCount { get; }

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    bool ConfigureConsumeTopology { get; }

    /// <summary>
    /// Gets the instance id value.
    /// </summary>
    string? InstanceId { get; }

    /// <summary>
    /// Configures endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    void ConfigureEndpoint<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator;
}
