using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides generic and assembly-scanning registration for Courier activities.</summary>
public static class CourierRegistrationExtensions
{
    /// <summary>Registers an execute-only Courier activity with its definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The argument type.</typeparam>
    /// <typeparam name="TDefinition">The activity definition type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The optional action that configures the execution pipeline.</param>
    /// <returns>A configurator for the registered execution-only activity.</returns>
    public static IExecuteActivityRegistrationConfigurator<TActivity, TArguments> AddExecuteActivity<TActivity, TArguments, TDefinition>(
        this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
        where TDefinition : class, IExecuteActivityDefinition<TActivity, TArguments>
    {
        return CourierRegistrationConfiguratorExtensions.AddExecuteActivity<TActivity, TArguments>(configurator, typeof(TDefinition), configure);
    }

    /// <summary>Registers a compensatable Courier activity with its definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The argument type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <typeparam name="TDefinition">The activity definition type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configureExecute">The execute configuration callback.</param>
    /// <param name="configureCompensate">The compensate configuration callback.</param>
    /// <returns>A configurator for the registered compensatable activity.</returns>
    public static IActivityRegistrationConfigurator<TActivity, TArguments, TLog> AddActivity<TActivity, TArguments, TLog, TDefinition>(
        this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>? configureExecute = null,
        Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>? configureCompensate = null)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TDefinition : class, IActivityDefinition<TActivity, TArguments, TLog>
        where TLog : class
    {
        return CourierRegistrationConfiguratorExtensions.AddActivity<TActivity, TArguments, TLog>(configurator, typeof(TDefinition),
            configureExecute, configureCompensate);
    }

    /// <summary>Adds all activities (including execute-only activities) in the specified assemblies.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="assemblies">The assemblies to scan for consumers.</param>
    public static void AddActivities(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Any(assembly => assembly is null))
            throw new ArgumentException("The assembly collection cannot contain null entries.", nameof(assemblies));

        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, IsActivityOrDefinition);

        AddActivities(configurator, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>Adds all activities (including execute-only activities) in the specified assemblies matching the namespace.</summary>
    /// <typeparam name="T">A type whose namespace identifies the scan boundary.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">An optional predicate that selects discovered activity types.</param>
    public static void AddActivitiesFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        AddActivitiesFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>Adds all activities (including execute-only activities) in the specified assemblies matching the namespace.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="filter">An optional predicate that selects discovered activity types.</param>
    public static void AddActivitiesFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(type);

        filter ??= _ => true;

        if (type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} does not have a namespace.", nameof(type));

        AddActivities(configurator, filter, FindTypesInNamespace(type, IsActivityOrDefinition));
    }

    /// <summary>Adds the specified activity types.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="types">The activity and activity-definition types to register.</param>
    public static void AddActivities(this IRegistrationConfigurator configurator, params Type[] types)
    {
        AddActivities(configurator, null, types);
    }

    /// <summary>Adds activities to the configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">An optional predicate that selects activity types.</param>
    /// <param name="types">The types.</param>
    public static void AddActivities(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(types);

        if (types.Any(type => type is null))
            throw new ArgumentException("The type collection cannot contain null entries.", nameof(types));

        filter ??= _ => true;

        Type[] activityTypes = types.Where(IsCompensatableActivity).ToArray();
        RegisterCompensatableActivities(configurator, filter, activityTypes, types.Where(IsCompensatableActivityDefinition));

        IEnumerable<Type> executeActivityTypes = types.Where(IsExecuteActivity).Except(activityTypes);
        RegisterExecuteOnlyActivities(configurator, filter, executeActivityTypes, types.Where(IsExecuteActivityDefinition));
    }


    static void RegisterCompensatableActivities(IRegistrationConfigurator configurator, Func<Type, bool> filter,
        IEnumerable<Type> activityTypes, IEnumerable<Type> definitionTypes)
    {
        var activities = from activityType in activityTypes
                         join definitionType in definitionTypes on activityType equals GetCompensatableActivityType(definitionType) into definitions
                         from definitionType in definitions.DefaultIfEmpty()
                         where filter(activityType)
                         select (ActivityType: activityType, DefinitionType: definitionType);

        foreach ((Type activityType, Type? definitionType) in activities)
            configurator.AddActivity(activityType, definitionType);
    }


    static void RegisterExecuteOnlyActivities(IRegistrationConfigurator configurator, Func<Type, bool> filter,
        IEnumerable<Type> activityTypes, IEnumerable<Type> definitionTypes)
    {
        var activities = from activityType in activityTypes
                         join definitionType in definitionTypes on activityType equals GetExecuteActivityType(definitionType) into definitions
                         from definitionType in definitions.DefaultIfEmpty()
                         where filter(activityType)
                         select (ActivityType: activityType, DefinitionType: definitionType);

        foreach ((Type activityType, Type? definitionType) in activities)
            configurator.AddExecuteActivity(activityType, definitionType);
    }


    static bool IsCompensatableActivity(Type type) => type.ImplementsInterface(typeof(IActivity<,>));

    static bool IsCompensatableActivityDefinition(Type type) => type.ImplementsInterface(typeof(IActivityDefinition<,,>));

    static bool IsExecuteActivity(Type type) => type.ImplementsInterface(typeof(IExecuteActivity<>));

    static bool IsExecuteActivityDefinition(Type type) => type.ImplementsInterface(typeof(IExecuteActivityDefinition<,>));

    static Type GetCompensatableActivityType(Type definitionType) =>
        definitionType.GetSingleClosedGenericArguments(typeof(IActivityDefinition<,,>)).First();

    static Type GetExecuteActivityType(Type definitionType) =>
        definitionType.GetSingleClosedGenericArguments(typeof(IExecuteActivityDefinition<,>)).First();


    static Type[] FindTypesInNamespace(Type type, Func<Type, bool> typeFilter)
    {
        if (type.Namespace == null)
            throw new ArgumentException("The type must have a valid namespace", nameof(type));

        var dottedNamespace = type.Namespace + ".";

        bool Filter(Type candidate)
        {
            return typeFilter(candidate)
                && candidate.Namespace != null
                && (candidate.Namespace.StartsWith(dottedNamespace, StringComparison.OrdinalIgnoreCase)
                    || candidate.Namespace.Equals(type.Namespace, StringComparison.OrdinalIgnoreCase));
        }

        return AssemblyTypeCache.FindTypes(type.Assembly, TypeClassification.Concrete | TypeClassification.Closed, Filter).ToArray();
    }

    static bool IsActivityOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return interfaces.Any(candidate => candidate.ImplementsInterface(typeof(IExecuteActivity<>))
            || candidate.ImplementsInterface(typeof(ICompensateActivity<>))
            || candidate.ImplementsInterface(typeof(IActivityDefinition<,,>))
            || candidate.ImplementsInterface(typeof(IExecuteActivityDefinition<,>)));
    }
}
