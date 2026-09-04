using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides an execute activity test harness implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityTestHarness<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityFactory<TActivity, TArguments> _activityFactory;
    readonly Action<IExecuteActivityConfigurator<TActivity, TArguments>> _configureExecute;
    Uri? _executeAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="testHarness">The test harness value.</param>
    /// <param name="activityFactory">The activity factory value.</param>
    /// <param name="configureExecute">The configure execute value.</param>
    public ExecuteActivityTestHarness(BusTestHarness testHarness, IExecuteActivityFactory<TActivity, TArguments> activityFactory,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>> configureExecute)
    {
        _configureExecute = configureExecute;
        _activityFactory = activityFactory;

        Name = GetActivityName();

        ExecuteQueueName = BuildQueueName("execute");

        testHarness.OnConfigureBus += ConfigureBus;
    }

    /// <summary>
    /// Gets or sets the execute queue name value.
    /// </summary>
    public string ExecuteQueueName { get; private set; }
    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Gets the execute address value.
    /// </summary>
    public Uri ExecuteAddress => _executeAddress ?? throw new InvalidOperationException("The execute activity test harness has not been configured.");

    /// <summary>
    /// Occurs when on configure execute receive endpoint.
    /// </summary>
    public event Action<IReceiveEndpointConfigurator>? OnConfigureExecuteReceiveEndpoint;

    void ConfigureBus(IBusFactoryConfigurator configurator)
    {
        configurator.ReceiveEndpoint(ExecuteQueueName, x =>
        {
            OnConfigureExecuteReceiveEndpoint?.Invoke(x);

            x.ExecuteActivityHost(_activityFactory, _configureExecute);

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
