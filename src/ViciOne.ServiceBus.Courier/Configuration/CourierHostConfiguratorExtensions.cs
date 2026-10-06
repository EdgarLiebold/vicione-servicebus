using System;
using ViciOne.ServiceBus.Courier;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures Courier activity hosts on receive endpoints.</summary>
public static class CourierHostConfiguratorExtensions
{
    /// <summary>Configures an execute-only activity created through its parameterless constructor.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>, new()
        where TArguments : class
    {
        ExecuteActivityHost(configurator, DefaultConstructorExecuteActivityFactory<TActivity, TArguments>.ExecuteFactory, configure);
    }

    /// <summary>Configures a compensatable activity created through its parameterless constructor.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        Uri compensateAddress, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>, new()
        where TArguments : class
    {
        ExecuteActivityHost(configurator, compensateAddress, DefaultConstructorExecuteActivityFactory<TActivity, TArguments>.ExecuteFactory, configure);
    }

    /// <summary>Configures a compensatable activity with a parameterless factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        Uri compensateAddress, Func<TActivity> activityFactory, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);
        ArgumentNullException.ThrowIfNull(activityFactory);

        ExecuteActivityHost(configurator, compensateAddress, _ => activityFactory(), configure);
    }

    /// <summary>Configures an execute-only activity with a parameterless factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        Func<TActivity> activityFactory, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(activityFactory);

        ExecuteActivityHost(configurator, _ => activityFactory(), configure);
    }

    /// <summary>Configures a compensatable activity with an argument-aware factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        Uri compensateAddress, Func<TArguments, TActivity> activityFactory,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);
        ArgumentNullException.ThrowIfNull(activityFactory);

        var factory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(activityFactory);

        ExecuteActivityHost(configurator, compensateAddress, factory, configure);
    }

    /// <summary>Configures an execute-only activity with an argument-aware factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        Func<TArguments, TActivity> activityFactory,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(activityFactory);

        var factory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(activityFactory);

        ExecuteActivityHost(configurator, factory, configure);
    }

    /// <summary>Configures a compensatable activity with an explicit activity factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, Uri compensateAddress,
        IExecuteActivityFactory<TActivity, TArguments> factory, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            LogContext.Debug?.Log("Configuring Execute Activity: {ActivityType}, {ArgumentType}", TypeCache<TActivity>.ShortName,
                TypeCache<TArguments>.ShortName);
        }
        catch
        {
            // Optional diagnostics cannot prevent valid activity specification admission.
        }

        var specification = new ExecuteActivityHostConfigurator<TActivity, TArguments>(factory, compensateAddress, configurator);

        configure?.Invoke(specification);

        configurator.AddEndpointSpecification(specification);
    }

    /// <summary>Configures an execute-only activity with an explicit activity factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator,
        IExecuteActivityFactory<TActivity, TArguments> factory, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            LogContext.Debug?.Log("Configuring Execute Activity: {ActivityType}, {ArgumentType}", TypeCache<TActivity>.ShortName,
                TypeCache<TArguments>.ShortName);
        }
        catch
        {
            // Optional diagnostics cannot prevent valid activity specification admission.
        }

        var specification = new ExecuteActivityHostConfigurator<TActivity, TArguments>(factory, configurator);

        configure?.Invoke(specification);

        configurator.AddEndpointSpecification(specification);
    }

    /// <summary>Configures compensation created through the activity's parameterless constructor.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void CompensateActivityHost<TActivity, TLog>(this IReceiveEndpointConfigurator configurator,
        Action<ICompensateActivityConfigurator<TActivity, TLog>>? configure = null)
        where TActivity : class, ICompensateActivity<TLog>, new()
        where TLog : class
    {
        CompensateActivityHost(configurator, DefaultConstructorCompensateActivityFactory<TActivity, TLog>.CompensateFactory, configure);
    }

    /// <summary>Configures compensation with a parameterless activity factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void CompensateActivityHost<TActivity, TLog>(this IReceiveEndpointConfigurator configurator, Func<TActivity> activityFactory,
        Action<ICompensateActivityConfigurator<TActivity, TLog>>? configure = null)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(activityFactory);

        CompensateActivityHost(configurator, _ => activityFactory(), configure);
    }

    /// <summary>Configures compensation with a log-aware activity factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void CompensateActivityHost<TActivity, TLog>(this IReceiveEndpointConfigurator configurator, Func<TLog, TActivity> activityFactory,
        Action<ICompensateActivityConfigurator<TActivity, TLog>>? configure = null)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(activityFactory);

        var factory = new FactoryMethodCompensateActivityFactory<TActivity, TLog>(activityFactory);

        CompensateActivityHost(configurator, factory, configure);
    }

    /// <summary>Configures compensation with an explicit activity factory.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void CompensateActivityHost<TActivity, TLog>(this IReceiveEndpointConfigurator configurator,
        ICompensateActivityFactory<TActivity, TLog> factory, Action<ICompensateActivityConfigurator<TActivity, TLog>>? configure = null)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            LogContext.Debug?.Log("Configuring Compensate Activity: {ActivityType}, {LogType}", TypeCache<TActivity>.ShortName,
                TypeCache<TLog>.ShortName);
        }
        catch
        {
            // Optional diagnostics cannot prevent valid activity specification admission.
        }

        var specification = new CompensateActivityHostConfigurator<TActivity, TLog>(factory, configurator);

        configure?.Invoke(specification);

        configurator.AddEndpointSpecification(specification);
    }
}
