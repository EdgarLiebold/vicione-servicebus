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
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
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
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
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
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.CompensateActivityConfigured(configurator));
    }
}
