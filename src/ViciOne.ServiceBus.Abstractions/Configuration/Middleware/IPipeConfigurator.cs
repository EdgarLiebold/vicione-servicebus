namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a pipe with specifications.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Adds a middleware specification at the end of the pipeline.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification(IPipeSpecification<TContext> specification);
}
