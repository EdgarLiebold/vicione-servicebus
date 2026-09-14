using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers consumer implementations and their optional definitions with a dependency-injection container.</summary>
public static class DependencyInjectionConsumerRegistrationExtensions
{
    /// <summary>Registers a consumer with the default container registrar.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <returns>The canonical registration for <typeparamref name="T" />.</returns>
    public static IConsumerRegistration RegisterConsumer<T>(this IServiceCollection collection)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterConsumer<T>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a consumer through the supplied container registrar.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <param name="registrar">The registrar that deduplicates component registrations.</param>
    /// <returns>The canonical registration for <typeparamref name="T" />.</returns>
    public static IConsumerRegistration RegisterConsumer<T>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(typeof(T)))
            throw new ArgumentException($"{TypeCache<T>.ShortName} is a saga, and cannot be registered as a consumer", nameof(T));

        return new ConsumerRegistrar<T>().Register(collection, registrar);
    }

    /// <summary>Registers a consumer and its endpoint definition with the default registrar.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <typeparam name="TDefinition">The consumer definition.</typeparam>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <returns>The canonical registration for <typeparamref name="T" />.</returns>
    public static IConsumerRegistration RegisterConsumer<T, TDefinition>(this IServiceCollection collection)
        where T : class, IConsumer
        where TDefinition : class, IConsumerDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterConsumer<T, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a consumer and its endpoint definition through the supplied registrar.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <typeparam name="TDefinition">The consumer definition.</typeparam>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <param name="registrar">The registrar that deduplicates component registrations.</param>
    /// <returns>The canonical registration for <typeparamref name="T" />.</returns>
    public static IConsumerRegistration RegisterConsumer<T, TDefinition>(this IServiceCollection collection, IContainerRegistrar registrar)
        where T : class, IConsumer
        where TDefinition : class, IConsumerDefinition<T>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(typeof(T)))
            throw new ArgumentException($"{TypeCache<T>.ShortName} is a saga, and cannot be registered as a consumer", nameof(T));

        return new ConsumerDefinitionRegistrar<T, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers a consumer with a runtime-selected definition through the default registrar.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <param name="consumerDefinitionType">The concrete consumer definition.</param>
    /// <returns>The canonical registration for <typeparamref name="T" />.</returns>
    public static IConsumerRegistration RegisterConsumer<T>(this IServiceCollection collection, Type consumerDefinitionType)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(consumerDefinitionType);

        return RegisterConsumer<T>(collection, new DependencyInjectionContainerRegistrar(collection), consumerDefinitionType);
    }

    /// <summary>Registers a consumer with an optional runtime definition through an explicit registrar.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <param name="registrar">The registrar that deduplicates component registrations.</param>
    /// <param name="consumerDefinitionType">The concrete consumer definition, or <see langword="null" /> to use convention defaults.</param>
    /// <returns>The canonical registration for <typeparamref name="T" />.</returns>
    public static IConsumerRegistration RegisterConsumer<T>(this IServiceCollection collection, IContainerRegistrar registrar, Type? consumerDefinitionType)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        if (consumerDefinitionType == null)
            return RegisterConsumer<T>(collection, registrar);

        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(typeof(T)))
            throw new ArgumentException($"{TypeCache<T>.ShortName} is a saga, and cannot be registered as a consumer", nameof(T));

        EnsureConsumerDefinitionType(consumerDefinitionType, typeof(T));

        var register = (IConsumerRegistrar)(Activator.CreateInstance(
            typeof(ConsumerDefinitionRegistrar<,>).MakeGenericType(typeof(T), consumerDefinitionType)) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    /// <summary>Registers a runtime-selected consumer and optional definition through an explicit registrar.</summary>
    /// <param name="collection">The service collection that owns the registration.</param>
    /// <param name="registrar">The registrar that deduplicates component registrations.</param>
    /// <param name="consumerType">The concrete consumer implementation.</param>
    /// <param name="consumerDefinitionType">The concrete consumer definition, or <see langword="null" /> to use convention defaults.</param>
    /// <returns>The canonical registration for <paramref name="consumerType" />.</returns>
    public static IConsumerRegistration RegisterConsumer(this IServiceCollection collection, IContainerRegistrar registrar, Type consumerType,
        Type? consumerDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(consumerType);

        EnsureConcreteConsumerType(consumerType);

        if (consumerDefinitionType != null)
        {
            EnsureConsumerDefinitionType(consumerDefinitionType, consumerType);

            var consumerRegistrar = (IConsumerRegistrar)(Activator.CreateInstance(
                typeof(ConsumerDefinitionRegistrar<,>).MakeGenericType(consumerType, consumerDefinitionType)) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));

            return consumerRegistrar.Register(collection, registrar);
        }

        var register = (IConsumerRegistrar)(Activator.CreateInstance(typeof(ConsumerRegistrar<>).MakeGenericType(consumerType)) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    static void EnsureConcreteConsumerType(Type consumerType)
    {
        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(consumerType))
            throw new ArgumentException($"{TypeCache.GetShortName(consumerType)} is a saga, and cannot be registered as a consumer", nameof(consumerType));
        if (!consumerType.IsClass || consumerType.IsAbstract || consumerType.ContainsGenericParameters
            || !ConsumerRegistrationMetadata.IsConsumer(consumerType))
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(consumerType)} is not a concrete consumer implementation",
                nameof(consumerType));
        }
    }

    static void EnsureConsumerDefinitionType(Type consumerDefinitionType, Type consumerType)
    {
        if (!consumerDefinitionType.IsClass || consumerDefinitionType.IsAbstract || consumerDefinitionType.ContainsGenericParameters
            || !consumerDefinitionType.TryGetSingleClosedGenericArguments(typeof(IConsumerDefinition<>), out Type[] types)
            || types[0] != consumerType)
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(consumerDefinitionType)} is not a consumer definition of {TypeCache.GetShortName(consumerType)}",
                nameof(consumerDefinitionType));
        }
    }


    interface IConsumerRegistrar
    {
        IConsumerRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    class ConsumerRegistrar<TConsumer> :
        IConsumerRegistrar
        where TConsumer : class, IConsumer
    {
        public virtual IConsumerRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);

            collection.TryAddScoped<TConsumer>();

            return registrar.GetOrAddRegistration<IConsumerRegistration>(typeof(TConsumer), _ => new ConsumerRegistration<TConsumer>(registrar));
        }
    }


    class ConsumerDefinitionRegistrar<TConsumer, TDefinition> :
        ConsumerRegistrar<TConsumer>
        where TDefinition : class, IConsumerDefinition<TConsumer>
        where TConsumer : class, IConsumer
    {
        public override IConsumerRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<IConsumerDefinition<TConsumer>, TDefinition>();

            return registration;
        }
    }
}
