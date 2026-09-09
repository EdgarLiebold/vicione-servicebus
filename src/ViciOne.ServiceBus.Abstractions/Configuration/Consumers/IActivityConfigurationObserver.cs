using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about activity configuration events.</summary>
public interface IActivityConfigurationObserver
{
    /// <summary>Observes a configured routing-slip activity that supports compensation.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
    /// <param name="configurator">The completed execute-activity configuration.</param>
    /// <param name="compensateAddress">The address of the compensation endpoint.</param>
    void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class;

    /// <summary>Observes a configured routing-slip execute activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
    /// <param name="configurator">The completed execute-activity configuration.</param>
    void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class;

    /// <summary>Observes a configured routing-slip compensation activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="configurator">The completed compensate-activity configuration.</param>
    void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class;
}
