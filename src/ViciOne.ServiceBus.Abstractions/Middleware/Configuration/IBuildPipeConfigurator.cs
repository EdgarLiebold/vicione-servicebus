namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for build pipe configurator.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IBuildPipeConfigurator<TContext> :
    IPipeConfigurator<TContext>,
    ISpecification
    where TContext : class, PipeContext
{
    /// <summary>
    /// Builds the pipe, applying any initial specifications to the front of the pipe
    /// </summary>
    /// <returns></returns>
    IPipe<TContext> Build();
}
