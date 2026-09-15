namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds topology filters while preserving delegated and implemented-message traversal state.</summary>
/// <typeparam name="T">The pipe context type.</typeparam>
public interface ITopologyPipeBuilder<T> :
    IPipeBuilder<T>
    where T : class, PipeContext
{
    /// <summary>
    /// Gets whether the builder represents topology delegated from another topology source.
    /// </summary>
    bool IsDelegated { get; }

    /// <summary>
    /// Gets whether the builder is traversing topology inherited from an implemented message contract.
    /// </summary>
    bool IsImplemented { get; }

    /// <summary>Creates a child builder that marks subsequent topology as delegated.</summary>
    /// <returns>The delegated child builder.</returns>
    ITopologyPipeBuilder<T> CreateDelegatedBuilder();
}
