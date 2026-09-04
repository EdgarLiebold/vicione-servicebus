using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Adds a fork to the pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class InterceptPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly IPipe<TContext> _pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    public InterceptPipeSpecification(IPipe<TContext> pipe)
    {
        _pipe = pipe;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(new InterceptFilter<TContext>(_pipe));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_pipe == null)
            yield return this.Failure("Pipe", "must not be null");
    }
}
