namespace ViciOne.ServiceBus.Configuration;

/// <summary>Collects ordered middleware specifications for a pipeline.</summary>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
public interface IPipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Appends a middleware specification to the pipeline configuration.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    void AddPipeSpecification(IPipeSpecification<TContext> specification);
}
