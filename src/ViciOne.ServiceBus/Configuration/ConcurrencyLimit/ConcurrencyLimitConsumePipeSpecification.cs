using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a concurrency limit filter to the message pipe.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConcurrencyLimitConsumePipeSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IConcurrencyLimiter _limiter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="limiter">The limiter.</param>
    public ConcurrencyLimitConsumePipeSpecification(IConcurrencyLimiter limiter)
    {
        _limiter = limiter;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        var filter = new ConsumeConcurrencyLimitFilter<T>(_limiter);

        builder.AddFilter(filter);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_limiter.Limit < 1)
            yield return this.Failure("ConcurrencyLimit", "must be >= 1");
    }
}
