using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides a test harness for activity test.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class ActivityTestHarness<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly IActivityFactory<TActivity, TArguments, TLog> _activityFactory;
    readonly Action<ICompensateActivityConfigurator<TActivity, TLog>> _configureCompensate;
    readonly Action<IExecuteActivityConfigurator<TActivity, TArguments>> _configureExecute;
    Uri? _compensateAddress;
    Uri? _executeAddress;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configureExecute">The configure execute.</param>
    /// <param name="configureCompensate">The configure compensate.</param>
    public ActivityTestHarness(BusTestHarness testHarness, IActivityFactory<TActivity, TArguments, TLog> activityFactory,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>> configureExecute,
        Action<ICompensateActivityConfigurator<TActivity, TLog>> configureCompensate)
    {
        _configureExecute = configureExecute;
        _configureCompensate = configureCompensate;
        _activityFactory = activityFactory;

        Name = GetActivityName();

        ExecuteQueueName = BuildQueueName("execute");
        CompensateQueueName = BuildQueueName("compensate");

        testHarness.OnConfigureBus += ConfigureBus;
    }

    /// <summary>Gets or sets the execute queue name.</summary>
    public string ExecuteQueueName { get; private set; }
    /// <summary>Gets or sets the compensate queue name.</summary>
    public string CompensateQueueName { get; private set; }
    /// <summary>Gets the compensate address.</summary>
    public Uri CompensateAddress => _compensateAddress ?? throw new InvalidOperationException("The activity test harness has not been configured.");
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; private set; }
    /// <summary>Gets the execute address.</summary>
    public Uri ExecuteAddress => _executeAddress ?? throw new InvalidOperationException("The activity test harness has not been configured.");

    /// <summary>Occurs when on configure execute receive endpoint.</summary>
    public event Action<IReceiveEndpointConfigurator>? OnConfigureExecuteReceiveEndpoint;
    /// <summary>Occurs when on configure compensate receive endpoint.</summary>
    public event Action<IReceiveEndpointConfigurator>? OnConfigureCompensateReceiveEndpoint;

    void ConfigureBus(IBusFactoryConfigurator configurator)
    {
        configurator.ReceiveEndpoint(CompensateQueueName, x =>
        {
            OnConfigureCompensateReceiveEndpoint?.Invoke(x);

            x.CompensateActivityHost(_activityFactory, _configureCompensate);

            _compensateAddress = x.InputAddress;
        });

        configurator.ReceiveEndpoint(ExecuteQueueName, x =>
        {
            OnConfigureExecuteReceiveEndpoint?.Invoke(x);

            x.ExecuteActivityHost(CompensateAddress, _activityFactory, _configureExecute);

            _executeAddress = x.InputAddress;
        });
    }

    static string GetActivityName()
    {
        var name = typeof(TActivity).Name;
        if (name.EndsWith("Activity"))
            name = name.Substring(0, name.Length - "Activity".Length);
        return name;
    }

    string BuildQueueName(string prefix)
    {
        return $"{prefix}_{typeof(TActivity).Name.ToLowerInvariant()}";
    }
}
