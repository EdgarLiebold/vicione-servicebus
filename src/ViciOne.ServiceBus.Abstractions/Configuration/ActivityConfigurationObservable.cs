using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for activity configuration.</summary>
public class ActivityConfigurationObservable :
    Connectable<IActivityConfigurationObserver>,
    IActivityConfigurationObserver
{
    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);

        ForEach(observer => observer.ActivityConfigured(configurator, compensateAddress));
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ExecuteActivityConfigured(configurator));
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.CompensateActivityConfigured(configurator));
    }
}
