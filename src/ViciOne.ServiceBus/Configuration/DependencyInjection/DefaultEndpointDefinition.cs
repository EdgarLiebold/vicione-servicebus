namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides default endpoint settings that specialized endpoint definitions can override.</summary>
public abstract class DefaultEndpointDefinition :
    IEndpointDefinition
{
    /// <summary>Creates a definition with the specified broker-resource lifetime.</summary>
    /// <param name="isTemporary">Whether the endpoint and its broker resources are removed when the endpoint stops.</param>
    protected DefaultEndpointDefinition(bool isTemporary = false)
    {
        IsTemporary = isTemporary;
    }

    /// <summary>Gets whether the transport creates consume topology for the endpoint.</summary>
    public virtual bool ConfigureConsumeTopology => true;

    /// <summary>Gets the endpoint name.</summary>
    /// <param name="formatter">The endpoint naming convention.</param>
    /// <returns>The endpoint name.</returns>
    public abstract string GetEndpointName(IEndpointNameFormatter formatter);

    /// <summary>Gets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    public virtual bool IsTemporary { get; }

    /// <summary>Gets the explicitly configured prefetch count, if any.</summary>
    public virtual int? PrefetchCount => default;

    /// <summary>Gets the explicitly configured concurrent-delivery limit, if any.</summary>
    public virtual int? ConcurrentMessageLimit => default;

    /// <summary>Validates the endpoint configurator; the default definition applies no additional settings.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The optional registration context.</param>
    public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
    }
}
