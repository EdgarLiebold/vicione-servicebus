using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for async delegate pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class AsyncDelegatePipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly Func<T, Task> _callback;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public AsyncDelegatePipeSpecification(Func<T, Task> callback)
    {
        _callback = callback;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        builder.AddFilter(new AsyncDelegateFilter<T>(_callback));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_callback == null)
            yield return this.Failure("Callback", "must not be null");
    }
}
