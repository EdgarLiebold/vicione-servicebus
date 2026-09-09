using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.RateLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds interval-based admission control to a pipeline.</summary>
/// <typeparam name="T">The pipeline context type.</typeparam>
internal sealed class RateLimitPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly TimeSpan _interval;
    readonly int _rateLimit;
    readonly IPipeRouter? _router;
    readonly TimeProvider _timeProvider;

    /// <summary>Captures the rate-limiting policy and its optional control route.</summary>
    /// <param name="rateLimit">The number of operations admitted per interval.</param>
    /// <param name="interval">The duration of each admission interval.</param>
    /// <param name="router">The optional control router used for runtime adjustments.</param>
    /// <param name="timeProvider">The clock and timer source used for replenishment.</param>
    public RateLimitPipeSpecification(int rateLimit, TimeSpan interval, IPipeRouter? router = null, TimeProvider? timeProvider = null)
    {
        _rateLimit = rateLimit;
        _interval = interval;
        _router = router;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var filter = new RateLimitFilter<T>(_rateLimit, _interval, _timeProvider);

        builder.AddFilter(filter);

        _router?.ConnectPipe(filter);
    }

    /// <summary>Validates the captured rate and interval.</summary>
    /// <returns>Every validation failure found in the policy.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rateLimit < 1)
            yield return this.Failure("RateLimit", "must be >= 1");
        if (_interval <= TimeSpan.Zero)
            yield return this.Failure("Interval", "must be > 0");
    }
}
