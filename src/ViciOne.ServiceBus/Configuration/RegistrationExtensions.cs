using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for core registration.</summary>
public static class RegistrationExtensions
{
    /// <summary>Adds the consumer, allowing configuration when it is configured on an endpoint.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <typeparam name="TDefinition">The consumer definition type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    public static IConsumerRegistrationConfigurator<T> AddConsumer<T, TDefinition>(this IRegistrationConfigurator configurator,
        Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
        where TDefinition : class, IConsumerDefinition<T>
    {
        return configurator.AddConsumer(typeof(TDefinition), configure);
    }

    /// <summary>Adds all consumers in the specified assemblies.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="assemblies">The assemblies to scan for consumers.</param>
    public static void AddConsumers(this IRegistrationConfigurator configurator, params Assembly[] assemblies)
    {
        AddConsumers(configurator, null, assemblies);
    }

    /// <summary>Adds all consumers that match the given filter in the specified assemblies.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for consumers.</param>
    public static void AddConsumers(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var types = AssemblyTypeCache.FindTypes(assemblies, ConsumerRegistrationMetadata.IsConsumerOrDefinition);

        AddConsumers(configurator, filter, types.FindTypes(TypeClassification.Concrete | TypeClassification.Closed).ToArray());
    }

    /// <summary>Adds all consumers from the assembly containing the specified type that are in the same (or deeper) namespace.</summary>
    /// <typeparam name="T">The anchor type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public static void AddConsumersFromNamespaceContaining<T>(this IRegistrationConfigurator configurator, Func<Type, bool>? filter = null)
    {
        AddConsumersFromNamespaceContaining(configurator, typeof(T), filter);
    }

    /// <summary>Adds all consumers in the specified assemblies matching the namespace.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="type">The type to use to identify the assembly and namespace to scan.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public static void AddConsumersFromNamespaceContaining(this IRegistrationConfigurator configurator, Type type, Func<Type, bool>? filter = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.Assembly == null || type.Namespace == null)
            throw new ArgumentException($"The type {TypeCache.GetShortName(type)} is not in an assembly with a valid namespace", nameof(type));

        AddConsumers(configurator, filter, FindTypesInNamespace(type, ConsumerRegistrationMetadata.IsConsumerOrDefinition));
    }

    /// <summary>Adds the specified consumer types.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="types">The consumer types to add.</param>
    public static void AddConsumers(this IRegistrationConfigurator configurator, params Type[] types)
    {
        AddConsumers(configurator, null, types);
    }

    /// <summary>Adds the specified consumer types which match the given filter.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="types">The consumer types to add.</param>
    public static void AddConsumers(this IRegistrationConfigurator configurator, Func<Type, bool>? filter, params Type[] types)
    {
        filter ??= t => true;

        IEnumerable<Type> consumerTypes = types.Where(ConsumerRegistrationMetadata.IsConsumer);
        IEnumerable<Type> consumerDefinitionTypes = types.Where(x => x.ImplementsInterface(typeof(IConsumerDefinition<>)));

        var consumers = from c in consumerTypes
                        join d in consumerDefinitionTypes on c equals d.GetSingleClosedGenericArgument(typeof(IConsumerDefinition<>)) into dc
                        from d in dc.DefaultIfEmpty()
                        where filter(c)
                        select new
                        {
                            ConsumerType = c,
                            DefinitionType = d
                        };

        foreach (var consumer in consumers)
            configurator.AddConsumer(consumer.ConsumerType, consumer.DefinitionType);
    }

    /// <summary>Configure the default endpoint name formatter in the container.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void SetDefaultEndpointNameFormatter(this IRegistrationConfigurator configurator)
    {
        configurator.SetEndpointNameFormatter(DefaultEndpointNameFormatter.Instance);
    }

    /// <summary>Sets snake case endpoint name formatter.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void SetSnakeCaseEndpointNameFormatter(this IRegistrationConfigurator configurator)
    {
        configurator.SetEndpointNameFormatter(SnakeCaseEndpointNameFormatter.Instance);
    }

    /// <summary>Configure the Kebab Case endpoint name formatter.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void SetKebabCaseEndpointNameFormatter(this IRegistrationConfigurator configurator)
    {
        configurator.SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);
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
