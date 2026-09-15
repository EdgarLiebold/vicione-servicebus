namespace ViciOne.ServiceBus.Configuration;

/// <summary>Appends filters while tracking which message specification layers may be applied.</summary>
/// <typeparam name="T">The pipeline context contract.</typeparam>
public interface ISpecificationPipeBuilder<T> :
    IPipeBuilder<T>
    where T : class, PipeContext
{
    /// <summary>
    /// Gets whether implemented-message-type specifications are suppressed for this builder.
    /// </summary>
    bool IsDelegated { get; }

    /// <summary>
    /// Gets whether base message specifications are suppressed for this builder.
    /// </summary>
    bool IsImplemented { get; }

    /// <summary>Creates a delegated builder while preserving the implemented marker.</summary>
    /// <returns>A builder that suppresses implemented-message-type specifications.</returns>
    ISpecificationPipeBuilder<T> CreateDelegatedBuilder();

    /// <summary>Creates an implemented builder while preserving the delegated marker.</summary>
    /// <returns>A builder that suppresses base message specifications.</returns>
    ISpecificationPipeBuilder<T> CreateImplementedBuilder();
}
