using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds an arbitrary filter to the pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class InlineFilterPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly InlineFilterMethod<TContext> _filterMethod;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filterMethod">The filter method.</param>
    public InlineFilterPipeSpecification(InlineFilterMethod<TContext> filterMethod)
    {
        _filterMethod = filterMethod;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(new InlineFilter<TContext>(_filterMethod));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filterMethod == null)
            yield return this.Failure("FilterMethod", "must not be null");
    }
}
