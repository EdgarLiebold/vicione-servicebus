using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Configures execute and compensation endpoints for a routing-slip activity under test.</summary>
/// <typeparam name="TActivity">The activity implementation.</typeparam>
/// <typeparam name="TArguments">The execute arguments.</typeparam>
/// <typeparam name="TLog">The compensation log.</typeparam>
public sealed class ActivityTestHarness<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly IActivityFactory<TActivity, TArguments, TLog> _activityFactory;
    readonly Action<ICompensateActivityConfigurator<TActivity, TLog>> _configureCompensate;
    readonly Action<IExecuteActivityConfigurator<TActivity, TArguments>> _configureExecute;
    Uri? _compensateAddress;
    Uri? _executeAddress;

    /// <summary>Registers an activity factory with the supplied bus test harness.</summary>
    /// <param name="testHarness">The bus harness that hosts the activity endpoints.</param>
    /// <param name="activityFactory">The factory that creates activity instances.</param>
    /// <param name="configureExecute">Configuration applied to the execute activity.</param>
    /// <param name="configureCompensate">Configuration applied to the compensation activity.</param>
    public ActivityTestHarness(BusTestHarness testHarness, IActivityFactory<TActivity, TArguments, TLog> activityFactory,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>> configureExecute,
        Action<ICompensateActivityConfigurator<TActivity, TLog>> configureCompensate)
    {
        ArgumentNullException.ThrowIfNull(testHarness);
        ArgumentNullException.ThrowIfNull(activityFactory);
        ArgumentNullException.ThrowIfNull(configureExecute);
        ArgumentNullException.ThrowIfNull(configureCompensate);

        _configureExecute = configureExecute;
        _configureCompensate = configureCompensate;
        _activityFactory = activityFactory;

        Name = GetActivityName();

        ExecuteQueueName = DefaultEndpointNameFormatter.Instance.ExecuteActivity<TActivity, TArguments>();
        CompensateQueueName = DefaultEndpointNameFormatter.Instance.CompensateActivity<TActivity, TLog>();

        testHarness.BusConfiguring += ConfigureBus;
    }

    /// <summary>Gets the execute queue name.</summary>
    public string ExecuteQueueName { get; }
    /// <summary>Gets the compensation queue name.</summary>
    public string CompensateQueueName { get; }
    /// <summary>Gets the compensation endpoint address after bus configuration completes.</summary>
    public Uri CompensateAddress => _compensateAddress ?? throw new InvalidOperationException("The activity test harness has not been configured.");
    /// <summary>Gets the activity name without its conventional <c>Activity</c> suffix.</summary>
    public string Name { get; }
    /// <summary>Gets the execute endpoint address after bus configuration completes.</summary>
    public Uri ExecuteAddress => _executeAddress ?? throw new InvalidOperationException("The activity test harness has not been configured.");

    /// <summary>Occurs while the execute receive endpoint is being configured.</summary>
    public event Action<IReceiveEndpointConfigurator>? ExecuteReceiveEndpointConfiguring;
    /// <summary>Occurs while the compensation receive endpoint is being configured.</summary>
    public event Action<IReceiveEndpointConfigurator>? CompensateReceiveEndpointConfiguring;

    void ConfigureBus(IBusFactoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.ReceiveEndpoint(CompensateQueueName, x =>
        {
            CompensateReceiveEndpointConfiguring?.Invoke(x);

            x.CompensateActivityHost(_activityFactory, _configureCompensate);

            _compensateAddress = x.InputAddress;
        });

        configurator.ReceiveEndpoint(ExecuteQueueName, x =>
        {
            ExecuteReceiveEndpointConfiguring?.Invoke(x);

            x.ExecuteActivityHost(CompensateAddress, _activityFactory, _configureExecute);

            _executeAddress = x.InputAddress;
        });
    }

    static string GetActivityName()
    {
        var name = typeof(TActivity).Name;
        if (name.EndsWith("Activity", StringComparison.Ordinal))
            name = name[..^"Activity".Length];
        return name;
    }

}
