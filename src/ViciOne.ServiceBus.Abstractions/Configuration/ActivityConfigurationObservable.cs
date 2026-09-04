using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an activity configuration observable implementation.
/// </summary>
public class ActivityConfigurationObservable :
    Connectable<IActivityConfigurationObserver>,
    IActivityConfigurationObserver
{
    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);

        ForEach(observer => observer.ActivityConfigured(configurator, compensateAddress));
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ExecuteActivityConfigured(configurator));
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.CompensateActivityConfigured(configurator));
    }
}
