using System;

namespace ViciOne.ServiceBus.Testing;

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

    public string ExecuteQueueName { get; private set; }
    public string CompensateQueueName { get; private set; }
    public Uri CompensateAddress => _compensateAddress ?? throw new InvalidOperationException("The activity test harness has not been configured.");
    public string Name { get; private set; }
    public Uri ExecuteAddress => _executeAddress ?? throw new InvalidOperationException("The activity test harness has not been configured.");

    public event Action<IReceiveEndpointConfigurator>? OnConfigureExecuteReceiveEndpoint;
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
