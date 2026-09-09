using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for activity configuration.</summary>
public class ActivityConfigurationObservable :
    Connectable<IActivityConfigurationObserver>,
    IActivityConfigurationObserver
{
    /// <summary>Notifies observers that an activity's execution and compensation pipelines are configured.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
    /// <param name="configurator">The completed execution-pipeline configuration.</param>
    /// <param name="compensateAddress">The endpoint that processes compensation requests.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);

        ForEach(observer => observer.ActivityConfigured(configurator, compensateAddress));
    }

    /// <summary>Notifies observers that an activity's execution pipeline is configured.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
    /// <param name="configurator">The completed execution-pipeline configuration.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.ExecuteActivityConfigured(configurator));
    }

    /// <summary>Notifies observers that an activity's compensation pipeline is configured.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="configurator">The completed compensation-pipeline configuration.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.CompensateActivityConfigured(configurator));
    }
}
