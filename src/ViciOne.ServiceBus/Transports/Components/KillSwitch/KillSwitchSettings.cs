namespace ViciOne.ServiceBus.Transports.Components;

using System;
using System.Collections.Generic;
using Configuration;


internal sealed record KillSwitchSettings(
    int ActivationThreshold,
    double TripThresholdRatio,
    TimeSpan TrackingPeriod,
    TimeSpan RestartDelay,
    TimeProvider TimeProvider,
    IExceptionFilter ExceptionFilter) :
    ISpecification
{
    public IEnumerable<ValidationResult> Validate()
    {
        if (ActivationThreshold < 1)
            yield return this.Failure(nameof(ActivationThreshold), "must be at least one");

        if (!double.IsFinite(TripThresholdRatio) || TripThresholdRatio is < 0 or > 1)
            yield return this.Failure(nameof(TripThresholdRatio), "must be between 0.0 and 1.0");

        if (TrackingPeriod <= TimeSpan.Zero)
            yield return this.Failure(nameof(TrackingPeriod), "must be greater than zero");

        if (RestartDelay < TimeSpan.FromSeconds(1))
            yield return this.Failure(nameof(RestartDelay), "must be at least one second");

        if (TimeProvider is null)
            yield return this.Failure(nameof(TimeProvider), "must not be null");

        if (ExceptionFilter is null)
            yield return this.Failure(nameof(ExceptionFilter), "must not be null");
    }
}
