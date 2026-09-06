using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a pipe with specifications.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Adds a pipe specification to the pipe configurator at the end of the chain.</summary>
    /// <param name="specification">The pipe specification to add.</param>
    void AddPipeSpecification(IPipeSpecification<TContext> specification);
}
