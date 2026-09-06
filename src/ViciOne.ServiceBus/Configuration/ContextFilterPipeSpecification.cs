using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for context filter pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ContextFilterPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly Func<TContext, Task<bool>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public ContextFilterPipeSpecification(Func<TContext, Task<bool>> filter)
    {
        _filter = filter;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(new ContextFilter<TContext>(_filter));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filter == null)
            yield return this.Failure("Filter", "must not be null");
    }
}
