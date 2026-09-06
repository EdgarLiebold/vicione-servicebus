using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Middleware.CircuitBreaker;

internal sealed record CircuitBreakerSettings(
    int MinimumThroughput,
    double FailureRatio,
    TimeSpan SamplingDuration,
    TimeSpan[] BreakDurations,
    TimeProvider TimeProvider,
    IExceptionFilter ExceptionFilter) : ISpecification
{
    public IEnumerable<ValidationResult> Validate()
    {
        if (MinimumThroughput < 1)
            yield return this.Failure(nameof(MinimumThroughput), "must be at least one");

        if (!double.IsFinite(FailureRatio) || FailureRatio is < 0 or > 1)
            yield return this.Failure(nameof(FailureRatio), "must be between 0.0 and 1.0");

        if (SamplingDuration <= TimeSpan.Zero)
            yield return this.Failure(nameof(SamplingDuration), "must be greater than zero");

        if (BreakDurations is null || BreakDurations.Length == 0)
            yield return this.Failure(nameof(BreakDurations), "must contain at least one duration");
        else if (BreakDurations.Any(duration => duration <= TimeSpan.Zero))
            yield return this.Failure(nameof(BreakDurations), "must contain only positive durations");

        if (TimeProvider is null)
            yield return this.Failure(nameof(TimeProvider), "must not be null");

        if (ExceptionFilter is null)
            yield return this.Failure(nameof(ExceptionFilter), "must not be null");
    }
}
