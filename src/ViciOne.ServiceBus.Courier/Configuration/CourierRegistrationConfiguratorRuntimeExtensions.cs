using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides typed and runtime Courier registration extensions.</summary>
public static class CourierRegistrationConfiguratorRuntimeExtensions
{
    /// <summary>Registers a compensatable Courier activity from its runtime type.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>A configurator for the registered compensatable activity.</returns>
    public static IActivityRegistrationConfigurator AddActivity(this IRegistrationConfigurator configurator, Type activityType,
        Type? activityDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(activityType);

        Type[] types = GetSingleContractArguments(
            activityType,
            typeof(IActivity<,>),
            "Courier activity");

        var register = (IRegisterActivity)(Activator.CreateInstance(typeof(RegisterActivity<,,>).MakeGenericType(activityType, types[0], types[1]))
            ?? throw new InvalidOperationException("The requested runtime activity registration could not be activated."));

        return register.Register(configurator, activityDefinitionType);
    }

    /// <summary>Registers an execute-only Courier activity from its runtime type.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>A configurator for the registered execution-only activity.</returns>
    public static IExecuteActivityRegistrationConfigurator AddExecuteActivity(this IRegistrationConfigurator configurator, Type activityType,
        Type? activityDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(activityType);

        Type[] types = GetSingleContractArguments(
            activityType,
            typeof(IExecuteActivity<>),
            "Courier execute activity");

        var register = (IRegisterExecuteActivity)(Activator.CreateInstance(typeof(RegisterExecuteActivity<,>).MakeGenericType(activityType, types[0]))
            ?? throw new InvalidOperationException("The requested runtime execute-activity registration could not be activated."));

        return register.Register(configurator, activityDefinitionType);
    }

    static Type[] GetSingleContractArguments(Type activityType, Type contractType, string contractDescription)
    {
        try
        {
            if (activityType.TryGetSingleClosedGenericArguments(contractType, out Type[] arguments))
                return arguments;
        }
        catch (InvalidOperationException exception)
        {
            throw new ArgumentException(
                $"The type must implement exactly one {contractDescription} contract: {TypeCache.GetShortName(activityType)}",
                nameof(activityType),
                exception);
        }

        throw new ArgumentException(
            $"The type is not a {contractDescription}: {TypeCache.GetShortName(activityType)}",
            nameof(activityType));
    }

    interface IRegisterActivity
    {
        IActivityRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? activityDefinitionType);
    }


    sealed class RegisterActivity<TActivity, TArguments, TLog> :
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


    sealed class RegisterExecuteActivity<TActivity, TArguments> :
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
