using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Configures an execute endpoint for an execute-only routing-slip activity under test.</summary>
/// <typeparam name="TActivity">The execute activity implementation.</typeparam>
/// <typeparam name="TArguments">The execute arguments.</typeparam>
public sealed class ExecuteActivityTestHarness<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityFactory<TActivity, TArguments> _activityFactory;
    readonly Action<IExecuteActivityConfigurator<TActivity, TArguments>> _configureExecute;
    Uri? _executeAddress;

    /// <summary>Registers an execute activity factory with the supplied bus test harness.</summary>
    /// <param name="testHarness">The bus harness that hosts the execute endpoint.</param>
    /// <param name="activityFactory">The factory that creates activity instances.</param>
    /// <param name="configureExecute">Configuration applied to the execute activity.</param>
    public ExecuteActivityTestHarness(BusTestHarness testHarness, IExecuteActivityFactory<TActivity, TArguments> activityFactory,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>> configureExecute)
    {
        ArgumentNullException.ThrowIfNull(testHarness);
        ArgumentNullException.ThrowIfNull(activityFactory);
        ArgumentNullException.ThrowIfNull(configureExecute);

        _configureExecute = configureExecute;
        _activityFactory = activityFactory;

        Name = GetActivityName();

        ExecuteQueueName = DefaultEndpointNameFormatter.Instance.ExecuteActivity<TActivity, TArguments>();

        testHarness.BusConfiguring += ConfigureBus;
    }

    /// <summary>Gets the execute queue name.</summary>
    public string ExecuteQueueName { get; }
    /// <summary>Gets the activity name without its conventional <c>Activity</c> suffix.</summary>
    public string Name { get; }
    /// <summary>Gets the execute endpoint address after bus configuration completes.</summary>
    public Uri ExecuteAddress => _executeAddress ?? throw new InvalidOperationException("The execute activity test harness has not been configured.");

    /// <summary>Occurs while the execute receive endpoint is being configured.</summary>
    public event Action<IReceiveEndpointConfigurator>? ExecuteReceiveEndpointConfiguring;

    void ConfigureBus(IBusFactoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.ReceiveEndpoint(ExecuteQueueName, x =>
        {
            ExecuteReceiveEndpointConfiguring?.Invoke(x);

            x.ExecuteActivityHost(_activityFactory, _configureExecute);

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
