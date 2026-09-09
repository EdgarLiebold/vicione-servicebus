using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a concurrency limit filter to the message pipe.</summary>
/// <typeparam name="T">The message type.</typeparam>
internal sealed class ConcurrencyLimitConsumePipeSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IConcurrencyLimiter _limiter;

    /// <summary>Creates a specification backed by the shared limiter.</summary>
    /// <param name="limiter">The concurrency budget shared with related consume pipelines.</param>
    public ConcurrencyLimitConsumePipeSpecification(IConcurrencyLimiter limiter)
    {
        _limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var filter = new ConsumeConcurrencyLimitFilter<T>(_limiter);

        builder.AddFilter(filter);
    }

    /// <summary>Validates that the shared limiter has a positive concurrency budget.</summary>
    /// <returns>The validation failure when the limit is not positive; otherwise an empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_limiter.Limit < 1)
            yield return this.Failure("ConcurrencyLimit", "must be >= 1");
    }
}
