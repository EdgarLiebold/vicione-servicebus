namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Base/Default endpoint definition, not used apparently
/// </summary>
public abstract class DefaultEndpointDefinition :
    IEndpointDefinition
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="isTemporary">The is temporary value.</param>
    protected DefaultEndpointDefinition(bool isTemporary = false)
    {
        IsTemporary = isTemporary;
    }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public virtual bool ConfigureConsumeTopology => true;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public abstract string GetEndpointName(IEndpointNameFormatter formatter);

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public virtual bool IsTemporary { get; }

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public virtual int? PrefetchCount => default;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public virtual int? ConcurrentMessageLimit => default;

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
