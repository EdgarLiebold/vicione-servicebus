using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds concurrency admission control and an optional runtime-control route to a pipeline.</summary>
/// <typeparam name="T">The pipeline context type.</typeparam>
internal sealed class ConcurrencyLimitPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly int _concurrencyLimit;
    readonly IPipeRouter? _router;

    /// <summary>Captures the concurrency limit and its optional control route.</summary>
    /// <param name="concurrencyLimit">The maximum number of concurrent downstream operations.</param>
    /// <param name="router">The optional control router used for runtime adjustments.</param>
    public ConcurrencyLimitPipeSpecification(int concurrencyLimit, IPipeRouter? router = null)
    {
        _concurrencyLimit = concurrencyLimit;
        _router = router;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var filter = new ConcurrencyLimitFilter<T>(_concurrencyLimit);

        builder.AddFilter(filter);

        _router?.ConnectPipe(filter);
    }

    /// <summary>Validates the captured concurrency limit.</summary>
    /// <returns>Every validation failure found in the policy.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_concurrencyLimit < 1)
            yield return this.Failure("ConcurrencyLimit", "must be >= 1");
    }
}
