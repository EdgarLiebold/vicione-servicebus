using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Adds an arbitrary filter to the pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class InlineFilterPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly InlineFilterMethod<TContext> _filterMethod;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filterMethod">The filter method value.</param>
    public InlineFilterPipeSpecification(InlineFilterMethod<TContext> filterMethod)
    {
        _filterMethod = filterMethod;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(new InlineFilter<TContext>(_filterMethod));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filterMethod == null)
            yield return this.Failure("FilterMethod", "must not be null");
    }
}
