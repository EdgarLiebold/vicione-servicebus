namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds specification pipe components.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ISpecificationPipeBuilder<T> :
    IPipeBuilder<T>
    where T : class, PipeContext
{
    /// <summary>
    /// If true, this is a delegated builder, and implemented message types
    /// and/or topology items should not be applied.
    /// </summary>
    bool IsDelegated { get; }

    /// <summary>
    /// If true, this is a builder for implemented types, so don't go down
    /// the rabbit hole twice.
    /// </summary>
    bool IsImplemented { get; }

    /// <summary>Creates delegated builder.</summary>
    /// <returns>The created delegated builder.</returns>
    ISpecificationPipeBuilder<T> CreateDelegatedBuilder();

    /// <summary>Creates implemented builder.</summary>
    /// <returns>The created implemented builder.</returns>
    ISpecificationPipeBuilder<T> CreateImplementedBuilder();
}
