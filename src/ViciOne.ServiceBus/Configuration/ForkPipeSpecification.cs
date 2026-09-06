using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a fork to the pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ForkPipeSpecification<TContext> :
    IPipeSpecification<TContext>
    where TContext : class, PipeContext
{
    readonly IPipe<TContext> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public ForkPipeSpecification(IPipe<TContext> pipe)
    {
        _pipe = pipe;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        builder.AddFilter(new ForkFilter<TContext>(_pipe));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_pipe == null)
            yield return this.Failure("Pipe", "must not be null");
    }
}
