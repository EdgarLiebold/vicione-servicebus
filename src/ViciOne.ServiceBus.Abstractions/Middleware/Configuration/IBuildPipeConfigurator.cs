namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures build pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IBuildPipeConfigurator<TContext> :
    IPipeConfigurator<TContext>,
    ISpecification
    where TContext : class, PipeContext
{
    /// <summary>Builds the pipe, applying any initial specifications to the front of the pipe.</summary>
    /// <returns>The configured component.</returns>
    IPipe<TContext> Build();
}
