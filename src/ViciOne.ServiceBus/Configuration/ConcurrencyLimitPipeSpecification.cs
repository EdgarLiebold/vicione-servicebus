using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a concurrency limit on the pipe. If the management endpoint is specified,
/// the consumer and appropriate mediator is created to handle the adjustment of the limit.
/// </summary>
/// <typeparam name="T">The message type being limited.</typeparam>
public class ConcurrencyLimitPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly int _concurrencyLimit;

    readonly IPipeRouter? _router = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="concurrencyLimit">The concurrency limit.</param>
    /// <param name="router">The router.</param>
    public ConcurrencyLimitPipeSpecification(int concurrencyLimit, IPipeRouter? router = null)
    {
        _concurrencyLimit = concurrencyLimit;

        _router = router;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        var filter = new ConcurrencyLimitFilter<T>(_concurrencyLimit);

        builder.AddFilter(filter);

        _router?.ConnectPipe(filter);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_concurrencyLimit < 1)
            yield return this.Failure("ConcurrencyLimit", "must be >= 1");
    }
}
