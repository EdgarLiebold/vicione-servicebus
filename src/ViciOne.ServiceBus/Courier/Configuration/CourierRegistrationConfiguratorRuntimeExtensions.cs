using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides typed and runtime Courier registration extensions.</summary>
public static class CourierRegistrationConfiguratorRuntimeExtensions
{
    /// <summary>Adds an activity (Courier), along with an optional activity definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    public static IActivityRegistrationConfigurator AddActivity(this IRegistrationConfigurator configurator, Type activityType,
        Type? activityDefinitionType = null)
    {
        if (!activityType.TryGetSingleClosedGenericArguments(typeof(IActivity<,>), out Type[] types))
            throw new ArgumentException($"The type is not a Courier activity: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        var register = (IRegisterActivity)(Activator.CreateInstance(typeof(RegisterActivity<,,>).MakeGenericType(activityType, types[0], types[1])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, activityDefinitionType);
    }

    /// <summary>Adds an execute activity (Courier), along with an optional activity definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>The execute activity registration configurator produced by the operation.</returns>
    public static IExecuteActivityRegistrationConfigurator AddExecuteActivity(this IRegistrationConfigurator configurator, Type activityType,
        Type? activityDefinitionType = null)
    {
        if (!activityType.TryGetSingleClosedGenericArguments(typeof(IExecuteActivity<>), out Type[] types))
            throw new ArgumentException($"The type is not a Courier execute activity: {TypeCache.GetShortName(activityType)}", nameof(activityType));

        var register = (IRegisterExecuteActivity)(Activator.CreateInstance(typeof(RegisterExecuteActivity<,>).MakeGenericType(activityType, types[0])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, activityDefinitionType);
    }

    interface IRegisterActivity
    {
        IActivityRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? activityDefinitionType);
    }


    class RegisterActivity<TActivity, TArguments, TLog> :
        IRegisterActivity
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        public IActivityRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? activityDefinitionType)
        {
            return CourierRegistrationConfiguratorExtensions.AddActivity<TActivity, TArguments, TLog>(configurator,
                activityDefinitionType);
        }
    }


    interface IRegisterExecuteActivity
    {
        IExecuteActivityRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? activityDefinitionType);
    }


    class RegisterExecuteActivity<TActivity, TArguments> :
        IRegisterExecuteActivity
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        public IExecuteActivityRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? activityDefinitionType)
        {
            return CourierRegistrationConfiguratorExtensions.AddExecuteActivity<TActivity, TArguments>(configurator,
                activityDefinitionType);
        }
    }
}
