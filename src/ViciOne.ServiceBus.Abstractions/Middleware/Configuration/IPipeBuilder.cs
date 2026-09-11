namespace ViciOne.ServiceBus.Configuration;

/// <summary>Constructs a pipeline by appending filters in execution order.</summary>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
public interface IPipeBuilder<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Appends a filter after the pipeline's existing filters.</summary>
    /// <param name="filter">The filter to add.</param>
    void AddFilter(IFilter<TContext> filter);
}
