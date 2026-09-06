using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers and registers futures through the application registration surface.</summary>
public static class FutureRegistrationExtensions
{
    /// <summary>Adds a future with a strongly typed definition.</summary>
    /// <typeparam name="T">The future state-machine type.</typeparam>
    /// <typeparam name="TDefinition">The future definition type.</typeparam>
    /// <param name="configurator">The application registration configurator.</param>
    /// <returns>A configurator for the registered future.</returns>
    public static IFutureRegistrationConfigurator<T> AddFuture<T, TDefinition>(this IRegistrationConfigurator configurator)
        where T : ViciOneServiceBusStateMachine<FutureState>
        where TDefinition : class, IFutureDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(configurator);
        return configurator.AddFuture<T>(typeof(TDefinition));
    }

    /// <summary>
    /// Adds a request consumer and a future hosted beside it. The future sends its command directly to
    /// the companion consumer and exposes the consumer's response as the durable future result.
    /// </summary>
    /// <typeparam name="TFuture">The future state-machine type.</typeparam>
    /// <typeparam name="TConsumer">The companion request consumer type.</typeparam>
    /// <typeparam name="TRequest">The request contract.</typeparam>
    /// <typeparam name="TResponse">The successful response contract.</typeparam>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="configure">The optional callback that configures the companion consumer.</param>
    /// <returns>A configurator for the registered future.</returns>
    public static IFutureRegistrationConfigurator<TFuture> AddFutureRequestConsumer<TFuture, TConsumer, TRequest, TResponse>(
        this IRegistrationConfigurator configurator, Action<IConsumerConfigurator<TConsumer>>? configure = null)
        where TFuture : Future<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
        where TConsumer : class, IConsumer<TRequest>
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.AddConsumer<TConsumer, FutureRequestConsumerDefinition<TConsumer, TRequest>>(configure);

        return configurator.AddFuture<TFuture, RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse>>();
    }

    /// <summary>Adds all futures found in the supplied assemblies, or in all loaded assemblies when none are supplied.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(assemblies);
        AddFutures(configurator, null, assemblies);
    }

    /// <summary>Adds matching futures from the supplied assemblies, or from all loaded assemblies when none are supplied.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="filter">The optional predicate applied to discovered future state-machine types.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(assemblies);
        if (assemblies.Any(static assembly => assembly is null))
            throw new ArgumentException("Assemblies must not contain null entries.", nameof(assemblies));

        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, FutureRegistrationMetadata.IsFutureOrDefinition);

        AddFutures(configurator, filter, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>Adds all futures from the assembly containing the specified type that are in the same (or deeper) namespace.</summary>
    /// <typeparam name="T">The anchor type.</typeparam>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="filter">The optional predicate applied to discovered future state-machine types.</param>
    public static void AddFuturesFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        AddFuturesFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>Adds futures from the anchor type's namespace and its child namespaces in the same assembly.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="type">The anchor type that identifies the assembly and root namespace.</param>
    /// <param name="filter">The optional predicate applied to discovered future state-machine types.</param>
    public static void AddFuturesFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(type);

        if (type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddFutures(configurator, filter, FindTypesInNamespace(type, FutureRegistrationMetadata.IsFutureOrDefinition));
    }

    /// <summary>Adds the specified future types.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="types">The future state-machine and definition types to add.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(types);
        AddFutures(configurator, null, types);
    }

    /// <summary>Adds the specified future types that match the given filter.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="filter">The optional predicate applied to future state-machine types.</param>
    /// <param name="types">The future state-machine and definition types to add.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(types);
        if (types.Any(static type => type is null))
            throw new ArgumentException("Types must not contain null entries.", nameof(types));

        filter ??= static _ => true;

        IEnumerable<Type> futureTypes = types.Where(x => x.ImplementsInterface(typeof(SagaStateMachine<FutureState>)));
        IEnumerable<Type> futureDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(IFutureDefinition<>)));

        var futures = from c in futureTypes
                      join d in futureDefinitionTypes on c equals d.GetSingleClosedGenericArgument(typeof(IFutureDefinition<>)) into dc
                      from d in dc.DefaultIfEmpty()
                      where filter(c)
                      select new
                      {
                          FutureType = c,
                          DefinitionType = d
                      };

        foreach (var future in futures)
            configurator.AddFuture(future.FutureType, future.DefinitionType);
    }


    static Type[] FindTypesInNamespace(Type type, Func<Type, bool> typeFilter)
    {
        if (type.Namespace == null)
            throw new ArgumentException("The anchor type must have a namespace.", nameof(type));

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
