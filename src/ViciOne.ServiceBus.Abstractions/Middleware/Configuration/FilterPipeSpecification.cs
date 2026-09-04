using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Adds an arbitrary filter to the pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class FilterPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext>? _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public FilterPipeSpecification(IFilter<TContext>? filter)
    {
        _filter = filter;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(_filter!);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filter == null)
            yield return this.Failure("Filter", "must not be null");
    }
}
