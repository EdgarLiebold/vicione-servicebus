using System;
using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds consumer registrations through the strongly typed and runtime-type APIs.</summary>
public static class RegistrationConfiguratorExtensions
{
    /// <summary>Registers a consumer with an optional endpoint-time callback.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="configure">The callback that configures the consumer when attached to an endpoint.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    public static IConsumerRegistrationConfigurator<T> AddConsumer<T>(this IRegistrationConfigurator configurator,
        Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configure != null ? configurator.AddConsumer<T>((_, cfg) => configure.Invoke(cfg)) : configurator.AddConsumer<T>();
    }

    /// <summary>Registers a consumer with a runtime-selected definition and optional endpoint-time callback.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="consumerDefinitionType">The concrete consumer definition.</param>
    /// <param name="configure">The callback that configures the consumer when attached to an endpoint.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    public static IConsumerRegistrationConfigurator<T> AddConsumer<T>(this IRegistrationConfigurator configurator,
        Type consumerDefinitionType, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerDefinitionType);

        return configure != null
            ? configurator.AddConsumer<T>(consumerDefinitionType, (_, cfg) => configure.Invoke(cfg))
            : configurator.AddConsumer<T>(consumerDefinitionType);
    }

    /// <summary>Registers a runtime-selected consumer and optional definition.</summary>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="consumerType">The concrete consumer implementation.</param>
    /// <param name="consumerDefinitionType">The concrete consumer definition, or <see langword="null" /> to use convention defaults.</param>
    /// <returns>A fluent configurator for the consumer registration.</returns>
    public static IConsumerRegistrationConfigurator AddConsumer(this IRegistrationConfigurator configurator, Type consumerType,
        Type? consumerDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerType);

        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(consumerType))
            throw new ArgumentException($"{TypeCache.GetShortName(consumerType)} is a saga, and cannot be registered as a consumer", nameof(consumerType));
        if (!consumerType.IsClass || consumerType.IsAbstract || consumerType.ContainsGenericParameters || !ConsumerRegistrationMetadata.IsConsumer(consumerType))
            throw new ArgumentException($"{TypeCache.GetShortName(consumerType)} is not a concrete consumer implementation", nameof(consumerType));

        var register = (IRegisterConsumer)(Activator.CreateInstance(typeof(RegisterConsumer<>).MakeGenericType(consumerType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, consumerDefinitionType);
    }

    interface IRegisterConsumer
    {
        IConsumerRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? consumerDefinitionType);
    }


    class RegisterConsumer<TConsumer> :
        IRegisterConsumer
        where TConsumer : class, IConsumer
    {
        public IConsumerRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? consumerDefinitionType)
        {
            return configurator.AddConsumer<TConsumer>(consumerDefinitionType);
        }
    }
}
