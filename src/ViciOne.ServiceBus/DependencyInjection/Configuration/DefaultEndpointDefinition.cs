namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides default endpoint settings that specialized endpoint definitions can override.</summary>
public abstract class DefaultEndpointDefinition :
    IEndpointDefinition
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="isTemporary">The is temporary.</param>
    protected DefaultEndpointDefinition(bool isTemporary = false)
    {
        IsTemporary = isTemporary;
    }

    /// <summary>Gets the configure consume topology.</summary>
    public virtual bool ConfigureConsumeTopology => true;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public abstract string GetEndpointName(IEndpointNameFormatter formatter);

    /// <summary>Gets a value indicating whether temporary.</summary>
    public virtual bool IsTemporary { get; }

    /// <summary>Gets the prefetch count.</summary>
    public virtual int? PrefetchCount => default;

    /// <summary>Gets the concurrent message limit.</summary>
    public virtual int? ConcurrentMessageLimit => default;

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
