using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about activity configuration events.</summary>
public interface IActivityConfigurationObserver
{
    /// <summary>Called when a routing slip activity that supports compensation host is configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The address of the compensation endpoint.</param>
    void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class;

    /// <summary>Called when a routing slip execute activity host is configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class;

    /// <summary>Called when a routing slip compensate activity host is configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class;
}
