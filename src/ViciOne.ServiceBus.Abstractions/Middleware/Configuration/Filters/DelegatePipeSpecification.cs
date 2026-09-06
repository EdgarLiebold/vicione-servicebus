using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for delegate pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class DelegatePipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly Action<T> _callback;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public DelegatePipeSpecification(Action<T> callback)
    {
        _callback = callback;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        builder.AddFilter(new DelegateFilter<T>(_callback));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_callback == null)
            yield return this.Failure("Callback", "must not be null");
    }
}
