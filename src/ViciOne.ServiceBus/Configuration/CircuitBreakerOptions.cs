using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.CircuitBreaker;

namespace ViciOne.ServiceBus.Configuration;
/// <summary>
/// Configures a circuit breaker. The values are captured as an immutable snapshot when the pipe is built.
/// </summary>
public sealed class CircuitBreakerOptions : IOptions
{
    private static readonly TimeSpan[] DefaultBreakDurations =
    [
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
    ];

    private ReadOnlyCollection<TimeSpan> _breakDurations;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public CircuitBreakerOptions()
    {
        MinimumThroughput = 5;
        FailureRatio = 0.05;
        SamplingDuration = TimeSpan.FromMinutes(1);
        _breakDurations = Array.AsReadOnly((TimeSpan[])DefaultBreakDurations.Clone());
        TimeProvider = TimeProvider.System;
        ExceptionFilter = new FilterSpecification().Build();
    }

    /// <summary>
    /// Minimum number of admitted operations in one sampling window before a matching failure may open the circuit.
    /// </summary>
    public int MinimumThroughput { get; private set; }

    /// <summary>
    /// Ratio of matching failures to admitted operations that opens the circuit, in the inclusive range 0.0 through 1.0.
    /// </summary>
    public double FailureRatio { get; private set; }

    /// <summary>
    /// Duration of the closed-state sampling window.
    /// </summary>
    public TimeSpan SamplingDuration { get; private set; }

    /// <summary>
    /// Bounded sequence of open-state durations. Repeated failures use the next value and then repeat the final value.
    /// </summary>
    public IReadOnlyList<TimeSpan> BreakDurations => _breakDurations;

    /// <summary>
    /// Time source used for sampling and recovery boundaries.
    /// </summary>
    public TimeProvider TimeProvider { get; private set; }

    /// <summary>
    /// Selects exceptions that count as protected-resource failures.
    /// </summary>
    public IExceptionFilter ExceptionFilter { get; private set; }

    /// <summary>
    /// Sets minimum throughput.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetMinimumThroughput(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        MinimumThroughput = value;
        return this;
    }

    /// <summary>
    /// Sets failure ratio.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetFailureRatio(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The failure ratio must be between 0.0 and 1.0.");

        FailureRatio = value;
        return this;
    }

    /// <summary>
    /// Sets sampling duration.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetSamplingDuration(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The sampling duration must be greater than zero.");

        SamplingDuration = value;
        return this;
    }

    /// <summary>
    /// Sets break duration.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetBreakDuration(TimeSpan value) => SetBreakDurations(value);

    /// <summary>
    /// Sets break durations.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetBreakDurations(params TimeSpan[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
            throw new ArgumentException("At least one break duration is required.", nameof(values));

        var snapshot = (TimeSpan[])values.Clone();
        if (snapshot.Any(value => value <= TimeSpan.Zero))
            throw new ArgumentOutOfRangeException(nameof(values), values, "Every break duration must be greater than zero.");

        _breakDurations = Array.AsReadOnly(snapshot);
        return this;
    }

    /// <summary>
    /// Sets time provider.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetTimeProvider(TimeProvider value)
    {
        TimeProvider = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <summary>
    /// Sets exception filter.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public CircuitBreakerOptions SetExceptionFilter(Action<IExceptionConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new FilterSpecification();
        configure(specification);
        ExceptionFilter = specification.Build();
        return this;
    }

    internal CircuitBreakerSettings CreateSettings() => new(
        MinimumThroughput,
        FailureRatio,
        SamplingDuration,
        [.. _breakDurations],
        TimeProvider,
        ExceptionFilter);

    private sealed class FilterSpecification : ExceptionSpecification
    {
        public IExceptionFilter Build() => CreateFilterSnapshot();
    }
}
