using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an async delegate pipe specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class AsyncDelegatePipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly Func<T, Task> _callback;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public AsyncDelegatePipeSpecification(Func<T, Task> callback)
    {
        _callback = callback;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        builder.AddFilter(new AsyncDelegateFilter<T>(_callback));
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
