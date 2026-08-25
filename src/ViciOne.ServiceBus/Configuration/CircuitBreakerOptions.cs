#nullable enable
namespace ViciOne.ServiceBus;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Configuration;
using Middleware;
using Middleware.CircuitBreaker;

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

    public CircuitBreakerOptions()
    {
        MinimumThroughput = 5;
        FailureRatio = 0.05;
        SamplingDuration = TimeSpan.FromMinutes(1);
        _breakDurations = Array.AsReadOnly((TimeSpan[])DefaultBreakDurations.Clone());
        TimeProvider = TimeProvider.System;
        ExceptionFilter = new ExceptionFilterBuilder().Build();
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

    public CircuitBreakerOptions SetMinimumThroughput(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        MinimumThroughput = value;
        return this;
    }

    public CircuitBreakerOptions SetFailureRatio(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The failure ratio must be between 0.0 and 1.0.");

        FailureRatio = value;
        return this;
    }

    public CircuitBreakerOptions SetSamplingDuration(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The sampling duration must be greater than zero.");

        SamplingDuration = value;
        return this;
    }

    public CircuitBreakerOptions SetBreakDuration(TimeSpan value) => SetBreakDurations(value);

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

    public CircuitBreakerOptions SetTimeProvider(TimeProvider value)
    {
        TimeProvider = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public CircuitBreakerOptions SetExceptionFilter(Action<IExceptionConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ExceptionFilterBuilder();
        configure(builder);
        ExceptionFilter = builder.Build();
        return this;
    }

    internal CircuitBreakerSettings CreateSettings() => new(
        MinimumThroughput,
        FailureRatio,
        SamplingDuration,
        [.. _breakDurations],
        TimeProvider,
        ExceptionFilter);

    private sealed class ExceptionFilterBuilder : IExceptionConfigurator
    {
        private readonly List<Func<Exception, bool>> _excludes = [];
        private readonly List<Func<Exception, bool>> _includes = [];

        public void Handle(params Type[] exceptionTypes)
        {
            Type[] snapshot = SnapshotTypes(exceptionTypes);
            _includes.Add(exception => Matches(exception, snapshot));
        }

        public void Handle<T>()
            where T : Exception => _includes.Add(static exception => Matches(exception, typeof(T)));

        public void Handle<T>(Func<T, bool> filter)
            where T : Exception
        {
            ArgumentNullException.ThrowIfNull(filter);
            _includes.Add(exception => Matches(exception, filter));
        }

        public void Ignore(params Type[] exceptionTypes)
        {
            Type[] snapshot = SnapshotTypes(exceptionTypes);
            _excludes.Add(exception => Matches(exception, snapshot));
        }

        public void Ignore<T>()
            where T : Exception => _excludes.Add(static exception => Matches(exception, typeof(T)));

        public void Ignore<T>(Func<T, bool> filter)
            where T : Exception
        {
            ArgumentNullException.ThrowIfNull(filter);
            _excludes.Add(exception => Matches(exception, filter));
        }

        public IExceptionFilter Build() => new SnapshotExceptionFilter([.. _includes], [.. _excludes]);

        private static Type[] SnapshotTypes(Type[] exceptionTypes)
        {
            ArgumentNullException.ThrowIfNull(exceptionTypes);

            var snapshot = (Type[])exceptionTypes.Clone();
            if (snapshot.Any(type => type is null || !typeof(Exception).IsAssignableFrom(type)))
                throw new ArgumentException("Every configured type must derive from Exception.", nameof(exceptionTypes));

            return snapshot;
        }

        private static bool Matches(Exception exception, params Type[] exceptionTypes)
        {
            Exception baseException = exception.GetBaseException();
            if (baseException is AggregateException aggregateException)
            {
                foreach (Exception innerException in aggregateException.InnerExceptions)
                {
                    Exception baseInnerException = innerException.GetBaseException();
                    foreach (Type exceptionType in exceptionTypes)
                    {
                        if (exceptionType.IsInstanceOfType(innerException)
                            || exceptionType.IsInstanceOfType(baseInnerException))
                            return true;
                    }
                }
            }

            foreach (Type exceptionType in exceptionTypes)
            {
                if (exceptionType.IsInstanceOfType(exception)
                    || exceptionType.IsInstanceOfType(baseException))
                    return true;
            }

            return false;
        }

        private static bool Matches<T>(Exception exception, Func<T, bool> filter)
            where T : Exception
        {
            if (exception is T typedException)
                return filter(typedException);

            Exception baseException = exception.GetBaseException();
            if (baseException is AggregateException aggregateException)
            {
                foreach (Exception innerException in aggregateException.InnerExceptions)
                {
                    if (innerException.GetBaseException() is T typedInnerException && filter(typedInnerException))
                        return true;
                }
            }

            return baseException is T typedBaseException && filter(typedBaseException);
        }
    }

    private sealed class SnapshotExceptionFilter(
        Func<Exception, bool>[] includes,
        Func<Exception, bool>[] excludes) : IExceptionFilter
    {
        public bool Match(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            bool included = includes.Length == 0;
            for (var index = 0; !included && index < includes.Length; index++)
                included = includes[index](exception);

            if (!included)
                return false;

            for (var index = 0; index < excludes.Length; index++)
            {
                if (excludes[index](exception))
                    return false;
            }

            return true;
        }

        void IProbeSite.Probe(ProbeContext context) => context.Add("filter", "snapshot");
    }
}
