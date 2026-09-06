using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for future registration.</summary>
public static class FutureRegistrationExtensions
{
    /// <summary>Adds the consumer, allowing configuration when it is configured on an endpoint.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <typeparam name="TDefinition">The consumer definition type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    public static IFutureRegistrationConfigurator<T> AddFuture<T, TDefinition>(this IRegistrationConfigurator configurator)
        where T : ViciOneServiceBusStateMachine<FutureState>
        where TDefinition : class, IFutureDefinition<T>
    {
        return configurator.AddFuture<T>(typeof(TDefinition));
    }

    /// <summary>
    /// Adds a combined consumer/future, where the future handles the requests and the consumer is only known to the future.
    /// This is a shortcut method,.
    /// </summary>
    /// <typeparam name="TFuture">The consumer type.</typeparam>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    public static IFutureRegistrationConfigurator<TFuture> AddFutureRequestConsumer<TFuture, TConsumer, TRequest, TResponse>(
        this IRegistrationConfigurator configurator, Action<IConsumerConfigurator<TConsumer>>? configure = null)
        where TFuture : Future<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
        where TConsumer : class, IConsumer<TRequest>
    {
        configurator.AddConsumer<TConsumer, FutureRequestConsumerDefinition<TConsumer, TRequest>>(configure);

        return configurator.AddFuture<TFuture, RequestConsumerFutureDefinition<TFuture, TConsumer, TRequest, TResponse>>();
    }

    /// <summary>Adds all futures in the specified assemblies.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="assemblies">The assemblies to scan for futures.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        AddFutures(configurator, null, assemblies);
    }

    /// <summary>Adds all futures that match the given filter in the specified assemblies.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for futures.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, FutureRegistrationMetadata.IsFutureOrDefinition);

        AddFutures(configurator, filter, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>Adds all futures from the assembly containing the specified type that are in the same (or deeper) namespace.</summary>
    /// <typeparam name="T">The anchor type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public static void AddFuturesFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        AddFuturesFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>Adds all futures in the specified assemblies matching the namespace.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public static void AddFuturesFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddFutures(configurator, filter, FindTypesInNamespace(type, FutureRegistrationMetadata.IsFutureOrDefinition));
    }

    /// <summary>Adds the specified consumer types.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="types">The state machine types to add.</param>
    /// ˆ
    public static void AddFutures(this IRegistrationConfigurator configurator, params Type[] types)
    {
        AddFutures(configurator, null, types);
    }

    /// <summary>Adds the specified consumer types which match the given filter.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="types">The consumer types to add.</param>
    public static void AddFutures(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        filter ??= t => true;

        IEnumerable<Type> consumerTypes = types.Where(x => x.ImplementsInterface(typeof(SagaStateMachine<FutureState>)));
        IEnumerable<Type> consumerDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(IFutureDefinition<>)));

        var futures = from c in consumerTypes
                      join d in consumerDefinitionTypes on c equals d.GetSingleClosedGenericArgument(typeof(IFutureDefinition<>)) into dc
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
}
