using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds an arbitrary filter to the pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class FilterPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext>? _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public FilterPipeSpecification(IFilter<TContext>? filter)
    {
        _filter = filter;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(_filter!);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filter == null)
            yield return this.Failure("Filter", "must not be null");
    }
}
