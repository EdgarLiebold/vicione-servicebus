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
    /// <returns>The execute activity registration configurator produced by the operation.</returns>
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
    /// <returns>The activity registration configurator produced by the operation.</returns>
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

        IEnumerable<Type> activityTypes = types.Where(x => x.ImplementsInterface(typeof(IActivity<,>))).ToList();
        IEnumerable<Type> activityDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(IActivityDefinition<,,>))).ToList();

        var activities = from c in activityTypes
                         join d in activityDefinitionTypes on c equals d.GetSingleClosedGenericArguments(typeof(IActivityDefinition<,,>)).First() into dc
                         from d in dc.DefaultIfEmpty()
                         where filter(c)
                         select new
                         {
                             ActivityType = c,
                             DefinitionType = d
                         };

        foreach (var activity in activities)
            configurator.AddActivity(activity.ActivityType, activity.DefinitionType);

        IEnumerable<Type> executeActivityTypes = types.Where(x => x.ImplementsInterface(typeof(IExecuteActivity<>))).Except(activityTypes).ToList();
        IEnumerable<Type> executeActivityDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(IExecuteActivityDefinition<,>))).ToList();

        var executeActivities = from c in executeActivityTypes
                                join d in executeActivityDefinitionTypes on c equals d.GetSingleClosedGenericArguments(typeof(IExecuteActivityDefinition<,>)).First() into dc
                                from d in dc.DefaultIfEmpty()
                                where filter(c)
                                select new
                                {
                                    ActivityType = c,
                                    DefinitionType = d
                                };

        foreach (var executeActivity in executeActivities)
            configurator.AddExecuteActivity(executeActivity.ActivityType, executeActivity.DefinitionType);
    }


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
