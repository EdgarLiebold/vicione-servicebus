using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a delegate pipe specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class DelegatePipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly Action<T> _callback;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public DelegatePipeSpecification(Action<T> callback)
    {
        _callback = callback;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        builder.AddFilter(new DelegateFilter<T>(_callback));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_callback == null)
            yield return this.Failure("Callback", "must not be null");
    }
}
