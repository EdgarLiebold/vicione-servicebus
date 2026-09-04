using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a rate limit pipe specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class RateLimitPipeSpecification<T> :
    IPipeSpecification<T>
    where T : class, PipeContext
{
    readonly TimeSpan _interval;
    readonly int _rateLimit;
    readonly IPipeRouter? _router = null!;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="rateLimit">The rate limit value.</param>
    /// <param name="interval">The interval value.</param>
    /// <param name="router">The router value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public RateLimitPipeSpecification(int rateLimit, TimeSpan interval, IPipeRouter? router = null, TimeProvider? timeProvider = null)
    {
        _rateLimit = rateLimit;
        _interval = interval;
        _router = router;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        var filter = new RateLimitFilter<T>(_rateLimit, _interval, _timeProvider);

        builder.AddFilter(filter);

        _router?.ConnectPipe(filter);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rateLimit < 1)
            yield return this.Failure("RateLimit", "must be >= 1");
        if (_interval <= TimeSpan.Zero)
            yield return this.Failure("Interval", "must be > 0");
    }
}
