namespace ViciOne.ServiceBus.Configuration;

/// <summary>Validates and applies one unit of pipeline configuration to a pipe builder.</summary>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
public interface IPipeSpecification<TContext> :
    ISpecification
    where TContext : class, PipeContext
{
    /// <summary>Applies the specification to the builder.</summary>
    /// <param name="builder">The pipe builder.</param>
    void Apply(IPipeBuilder<TContext> builder);
}
