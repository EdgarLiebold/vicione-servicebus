namespace ViciOne.ServiceBus;

using System;
using Configuration;
using Transports.Components;


/// <summary>
/// Configures the endpoint kill switch. The configuration is captured as an immutable snapshot
/// when <see cref="KillSwitchConfigurationExtensions.UseKillSwitch(IBusFactoryConfigurator, Action{KillSwitchOptions}?)" />
/// or <see cref="KillSwitchConfigurationExtensions.UseKillSwitch(IReceiveEndpointConfigurator, Action{KillSwitchOptions}?)" />
/// returns.
/// </summary>
public sealed class KillSwitchOptions :
    IOptions
{
    public KillSwitchOptions()
    {
        ActivationThreshold = 100;
        TripThresholdRatio = 0.10;
        TrackingPeriod = TimeSpan.FromMinutes(1);
        RestartDelay = TimeSpan.FromMinutes(1);
        TimeProvider = TimeProvider.System;
        ExceptionFilter = new FilterSpecification().Build();
    }

    /// <summary>
    /// Minimum number of observed delivery attempts before the switch may trip.
    /// </summary>
    public int ActivationThreshold { get; private set; }

    /// <summary>
    /// Ratio of matching failures to all observed attempts that trips the switch, in the inclusive range 0.0 through 1.0.
    /// </summary>
    public double TripThresholdRatio { get; private set; }

    /// <summary>
    /// Rolling observation window. Counters are reset lazily on the first observation after this duration.
    /// </summary>
    public TimeSpan TrackingPeriod { get; private set; }

    /// <summary>
    /// Delay between a successful pause and the next restart attempt. The same bounded delay is used before retrying a failed pause.
    /// </summary>
    public TimeSpan RestartDelay { get; private set; }

    /// <summary>
    /// Time source used for the observation window and recovery delay.
    /// </summary>
    public TimeProvider TimeProvider { get; private set; }

    /// <summary>
    /// Exception filter used to decide which consumer and routing-slip failures contribute to the failure ratio.
    /// </summary>
    public IExceptionFilter ExceptionFilter { get; private set; }

    public KillSwitchOptions SetActivationThreshold(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        ActivationThreshold = value;
        return this;
    }

    public KillSwitchOptions SetTripThresholdRatio(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The trip threshold ratio must be between 0.0 and 1.0.");

        TripThresholdRatio = value;
        return this;
    }

    public KillSwitchOptions SetTrackingPeriod(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), value, "The tracking period must be greater than zero.");

        TrackingPeriod = value;
        return this;
    }

    public KillSwitchOptions SetRestartDelay(TimeSpan value)
    {
        if (value < TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(value), value, "The restart delay must be at least one second.");

        RestartDelay = value;
        return this;
    }

    public KillSwitchOptions SetTimeProvider(TimeProvider value)
    {
        TimeProvider = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public KillSwitchOptions SetExceptionFilter(Action<IExceptionConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new FilterSpecification();
        configure(specification);
        ExceptionFilter = specification.Build();
        return this;
    }

    internal KillSwitchSettings CreateSettings() => new(
        ActivationThreshold,
        TripThresholdRatio,
        TrackingPeriod,
        RestartDelay,
        TimeProvider,
        ExceptionFilter);


    private sealed class FilterSpecification :
        ExceptionSpecification
    {
        public IExceptionFilter Build() => Filter;
    }
}
