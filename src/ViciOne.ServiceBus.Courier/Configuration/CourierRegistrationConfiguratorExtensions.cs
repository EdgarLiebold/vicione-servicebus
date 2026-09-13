using System;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the activity registration surface contributed by the Courier capability package.</summary>
public static class CourierRegistrationConfiguratorExtensions
{
    /// <summary>Adds Courier runtime services and message-correlation conventions to this bus registration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The same registration configurator for fluent composition.</returns>
    public static IRegistrationConfigurator AddCourier(this IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        CourierServiceRegistration.Register(configurator.Services, configurator.BusType);
        return configurator;
    }

    /// <summary>Adds an execute activity and allows it to be configured when attached to an endpoint.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The optional action that configures the execution pipeline.</param>
    /// <returns>A configurator for the registered execution-only activity.</returns>
    public static IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(
        this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class =>
        configurator.AddExecuteActivity(null, configure);

    /// <summary>Adds an execute activity with an optional definition and allows it to be configured when attached to an endpoint.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="executeActivityDefinitionType">The runtime execute activity definition type used by the operation.</param>
    /// <param name="configure">The optional action that configures the execution pipeline.</param>
    /// <returns>A configurator for the registered execution-only activity.</returns>
    public static IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments>(
        this IRegistrationConfigurator configurator, Type? executeActivityDefinitionType,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (typeof(TActivity).ImplementsInterface(typeof(IActivity<,>)))
        {
            throw new ArgumentException(
                $"Compensatable Courier activities must be registered using AddActivity: {TypeCache<TActivity>.ShortName}");
        }

        CourierServiceRegistration.Register(configurator.Services, configurator.BusType);
        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        IExecuteActivityRegistration registration = configurator.Services.RegisterExecuteActivity<TActivity, TArguments>(advanced.Registrar,
            executeActivityDefinitionType);
        registration.AddConfigureAction(configure);
        return new ExecuteActivityRegistrationConfigurator<TActivity, TArguments>(configurator, registration);
    }

    /// <summary>Adds a compensatable activity and allows its execute and compensate behaviors to be configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configureExecute">The optional action that configures the execution pipeline.</param>
    /// <param name="configureCompensate">The optional action that configures the compensation pipeline.</param>
    /// <returns>A configurator for the registered compensatable activity.</returns>
    public static IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog>(
        this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class =>
        configurator.AddActivity(null, configureExecute, configureCompensate);

    /// <summary>Adds a compensatable activity with an optional definition and allows its behaviors to be configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <param name="configureExecute">The optional action that configures the execution pipeline.</param>
    /// <param name="configureCompensate">The optional action that configures the compensation pipeline.</param>
    /// <returns>A configurator for the registered compensatable activity.</returns>
    public static IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog>(
        this IRegistrationConfigurator configurator, Type? activityDefinitionType,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        CourierServiceRegistration.Register(configurator.Services, configurator.BusType);
        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        IActivityRegistration registration = configurator.Services.RegisterActivity<TActivity, TArguments, TLog>(advanced.Registrar,
            activityDefinitionType);
        registration.AddConfigureAction(configureExecute);
        registration.AddConfigureAction(configureCompensate);
        return new ActivityRegistrationConfigurator<TActivity, TArguments, TLog>(configurator, registration);
    }
}
