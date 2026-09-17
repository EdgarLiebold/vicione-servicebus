using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for saga registration.</summary>
public static class SagaRegistrationExtensions
{
    /// <summary>
    /// Adds a class-based saga and allows endpoint configuration. State-machine sagas are registered
    /// through the dedicated <c>AddSagaStateMachine</c> family.
    /// </summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <typeparam name="TDefinition">The saga definition type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<T> AddSaga<T, TDefinition>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
        where TDefinition : class, ISagaDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return SagaRegistrationConfiguratorExtensions.AddSaga(configurator, typeof(TDefinition), configure);
    }

    /// <summary>Adds all sagas found in the assemblies currently loaded in the application domain.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddSagas(this IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        AddSagas(configurator, Array.Empty<Assembly>());
    }

    /// <summary>Adds matching sagas found in the assemblies currently loaded in the application domain.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The required predicate applied to discovered saga types.</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, Func<Type, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);

        AddSagas(configurator, filter, Array.Empty<Assembly>());
    }

    /// <summary>Adds all sagas in the specified assemblies. If using state machine sagas, they should be added first using AddSagaStateMachines.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The required predicate applied to discovered saga types; it cannot be <see langword="null" />.</param>
    /// <param name="assemblies">The assemblies to scan for consumers.</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, Func<Type, bool> filter, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);
        ValidateAssemblies(assemblies);

        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, SagaRegistrationMetadata.IsSagaOrDefinition);

        AddSagas(configurator, filter, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>Adds all sagas in the specified assemblies. If using state machine sagas, they should be added first using AddSagaStateMachines.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="assemblies">The assemblies to scan for consumers.</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateAssemblies(assemblies);

        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, SagaRegistrationMetadata.IsSagaOrDefinition);

        AddSagas(configurator, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>
    /// Adds all sagas in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagaStateMachinesFromNamespaceContaining prior to calling this one.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">An optional predicate applied to saga types; <see langword="null" /> accepts every candidate.</param>
    public static void AddSagasFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        AddSagasFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>
    /// Adds all sagas in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagaStateMachinesFromNamespaceContaining prior to calling this one.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="filter">An optional predicate applied to saga types; <see langword="null" /> accepts every candidate.</param>
    public static void AddSagasFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(type);

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddSagas(configurator, filter, FindTypesInNamespace(type, SagaRegistrationMetadata.IsSagaOrDefinition));
    }

    /// <summary>Adds the specified saga and saga definition types.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="types">The state machine types to add.</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTypes(types);

        AddSagas(configurator, null, types);
    }

    /// <summary>Adds the specified saga types.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">An optional predicate applied to saga types; <see langword="null" /> accepts every candidate.</param>
    /// <param name="types">The state machine types to add.</param>
    public static void AddSagas(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTypes(types);

        Func<Type, bool> sagaFilter = filter ?? (static _ => true);
        (Type SagaType, Type? DefinitionType)[] registrations = PlanSagaRegistrations(types, sagaFilter);

        foreach ((Type sagaType, Type? definitionType) in registrations)
            configurator.AddSaga(sagaType, definitionType);
    }

    /// <summary>
    /// Adds a SagaStateMachine to the registry and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="T">The state machine instance type.</typeparam>
    /// <typeparam name="TDefinition">The saga definition type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<T> AddSagaStateMachine<TStateMachine, T, TDefinition>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<T>>? configure = null)
        where T : class, ISagaStateMachineInstance
        where TStateMachine : class, ISagaStateMachine<T>
        where TDefinition : class, ISagaDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<TStateMachine, T>(configurator, typeof(TDefinition), configure);
    }

    /// <summary>Adds all saga state machines found in the assemblies currently loaded in the application domain.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        AddSagaStateMachines(configurator, Array.Empty<Assembly>());
    }

    /// <summary>
    /// Adds SagaStateMachines to the registry, using the factory method, and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="assemblies">The assemblies to scan for state machines.</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateAssemblies(assemblies);

        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies,
            type => SagaRegistrationMetadata.IsSagaStateMachineOrDefinition(type) && !SagaRegistrationMetadata.IsConsumerKindOwned(type));

        configurator.AddSagaStateMachines(types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>
    /// Adds all saga state machines in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagasFromNamespaceContaining after calling this one.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">
    /// An optional predicate applied to both state-machine and saga-state types. A candidate is selected when either type matches;
    /// <see langword="null" /> accepts every candidate.
    /// </param>
    public static void AddSagaStateMachinesFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        AddSagaStateMachinesFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>
    /// Adds all saga state machines in the specified assemblies matching the namespace. If you are using both state machine and regular sagas, be
    /// sure to call AddSagasFromNamespaceContaining after calling this one.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="filter">
    /// An optional predicate applied to both state-machine and saga-state types. A candidate is selected when either type matches;
    /// <see langword="null" /> accepts every candidate.
    /// </param>
    public static void AddSagaStateMachinesFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(type);

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddSagaStateMachines(configurator, filter,
            FindTypesInNamespace(type, x => SagaRegistrationMetadata.IsSagaStateMachineOrDefinition(x) && !SagaRegistrationMetadata.IsConsumerKindOwned(x)));
    }

    /// <summary>
    /// Adds SagaStateMachines to the registry, using the factory method, and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="types">The state machine types to add.</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTypes(types);

        AddSagaStateMachines(configurator, null, types);
    }

    /// <summary>
    /// Adds SagaStateMachines to the registry, using the factory method, and updates the registrar prior to registering so that the default
    /// saga registrar isn't notified.
    /// </summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">
    /// An optional predicate applied to both the state-machine type and its saga-state type. A candidate is selected when either type
    /// matches; <see langword="null" /> accepts every candidate.
    /// </param>
    /// <param name="types">The state machine types to add.</param>
    public static void AddSagaStateMachines(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateTypes(types);

        Func<Type, bool> sagaFilter = filter ?? (static _ => true);
        (Type StateMachineType, Type? DefinitionType)[] registrations = PlanStateMachineRegistrations(types, sagaFilter);

        foreach ((Type stateMachineType, Type? definitionType) in registrations)
            configurator.AddSagaStateMachine(stateMachineType, definitionType);
    }


    static (Type SagaType, Type? DefinitionType)[] PlanSagaRegistrations(Type[] types, Func<Type, bool> filter)
    {
        Type[] candidates = types.Distinct().Where(IsClosedConcreteClass).ToArray();
        Type[] sagaTypes = candidates
            .Where(type => type.ImplementsInterface<ISaga>() && !type.ImplementsInterface<ISagaStateMachineInstance>())
            .Where(filter)
            .ToArray();
        (Type DefinitionType, Type SagaType)[] definitions = GetDefinitionMappings(candidates);

        Dictionary<Type, Type> definitionsBySaga = GetDefinitionsBySaga(sagaTypes, definitions);

        return sagaTypes
            .Select(sagaType =>
            {
                definitionsBySaga.TryGetValue(sagaType, out Type? definitionType);
                return (SagaType: sagaType, DefinitionType: definitionType);
            })
            .ToArray();
    }

    static (Type StateMachineType, Type? DefinitionType)[] PlanStateMachineRegistrations(Type[] types, Func<Type, bool> filter)
    {
        Type[] candidates = types
            .Distinct()
            .Where(IsClosedConcreteClass)
            .Where(type => !SagaRegistrationMetadata.IsConsumerKindOwned(type))
            .ToArray();
        var stateMachines = new List<(Type StateMachineType, Type SagaType)>();
        foreach (Type stateMachineType in candidates.Where(type => type.ImplementsInterface(typeof(ISagaStateMachine<>))))
        {
            Type sagaType;
            try
            {
                sagaType = stateMachineType.GetSingleClosedGenericArgument(typeof(ISagaStateMachine<>));
            }
            catch (InvalidOperationException exception)
            {
                throw new ArgumentException(
                    $"{TypeCache.GetShortName(stateMachineType)} must implement exactly one ISagaStateMachine<TSaga> contract",
                    "types",
                    exception);
            }

            if (filter(stateMachineType) || filter(sagaType))
                stateMachines.Add((stateMachineType, sagaType));
        }

        (Type DefinitionType, Type SagaType)[] definitions = GetDefinitionMappings(candidates);
        Dictionary<Type, Type> definitionsBySaga = GetDefinitionsBySaga(
            stateMachines.Select(stateMachine => stateMachine.SagaType),
            definitions);

        return stateMachines
            .Select(stateMachine =>
            {
                definitionsBySaga.TryGetValue(stateMachine.SagaType, out Type? definitionType);
                return (stateMachine.StateMachineType, DefinitionType: definitionType);
            })
            .ToArray();
    }

    static (Type DefinitionType, Type SagaType)[] GetDefinitionMappings(IEnumerable<Type> candidates)
    {
        var definitions = new List<(Type DefinitionType, Type SagaType)>();
        foreach (Type definitionType in candidates.Where(type => type.ImplementsInterface(typeof(ISagaDefinition<>))))
        {
            try
            {
                definitions.Add((definitionType, definitionType.GetSingleClosedGenericArgument(typeof(ISagaDefinition<>))));
            }
            catch (InvalidOperationException exception)
            {
                throw new ArgumentException(
                    $"{TypeCache.GetShortName(definitionType)} must implement exactly one ISagaDefinition<TSaga> contract",
                    "types",
                    exception);
            }
        }

        return definitions.ToArray();
    }

    static Dictionary<Type, Type> GetDefinitionsBySaga(
        IEnumerable<Type> selectedSagaTypes,
        IEnumerable<(Type DefinitionType, Type SagaType)> definitions)
    {
        var selected = selectedSagaTypes.ToHashSet();
        (Type DefinitionType, Type SagaType)[] selectedDefinitions = definitions
            .Where(definition => selected.Contains(definition.SagaType))
            .ToArray();

        foreach (IGrouping<Type, (Type DefinitionType, Type SagaType)> group in selectedDefinitions.GroupBy(x => x.SagaType))
        {
            if (group.Skip(1).Any())
            {
                throw new ArgumentException(
                    $"Multiple saga definitions target {TypeCache.GetShortName(group.Key)}",
                    "types");
            }
        }

        return selectedDefinitions.ToDictionary(
            definition => definition.SagaType,
            definition => definition.DefinitionType);
    }

    static bool IsClosedConcreteClass(Type type) =>
        type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters;

    static void ValidateAssemblies(Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        if (assemblies.Any(static assembly => assembly is null))
            throw new ArgumentException("Assemblies must not contain null entries.", nameof(assemblies));
    }

    static void ValidateTypes(Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        if (types.Any(static type => type is null))
            throw new ArgumentException("Types must not contain null entries.", nameof(types));
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
                && (candidate.Namespace.StartsWith(dottedNamespace, StringComparison.Ordinal)
                    || candidate.Namespace.Equals(type.Namespace, StringComparison.Ordinal));
        }

        return AssemblyTypeCache.FindTypes(type.Assembly, TypeClassification.Concrete | TypeClassification.Closed, Filter).ToArray();
    }
}
