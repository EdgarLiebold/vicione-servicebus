using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for core registration configurators.</summary>
public static class RegistrationConfiguratorExtensions
{
    /// <summary>Adds the consumer, allowing configuration when it is configured on an endpoint.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    public static IConsumerRegistrationConfigurator<T> AddConsumer<T>(this IRegistrationConfigurator configurator,
        Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        return configure != null ? configurator.AddConsumer<T>((_, cfg) => configure.Invoke(cfg)) : configurator.AddConsumer<T>();
    }

    /// <summary>Adds the consumer, allowing configuration when it is configured on an endpoint.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="consumerDefinitionType">The consumer definition type.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    public static IConsumerRegistrationConfigurator<T> AddConsumer<T>(this IRegistrationConfigurator configurator,
        Type consumerDefinitionType, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        return configure != null
            ? configurator.AddConsumer<T>(consumerDefinitionType, (_, cfg) => configure.Invoke(cfg))
            : configurator.AddConsumer<T>(consumerDefinitionType);
    }

    /// <summary>Adds the consumer, along with an optional consumer definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="consumerType">The consumer type.</param>
    /// <param name="consumerDefinitionType">The consumer definition type.</param>
    /// <returns>The consumer registration configurator produced by the operation.</returns>
    public static IConsumerRegistrationConfigurator AddConsumer(this IRegistrationConfigurator configurator, Type consumerType,
        Type? consumerDefinitionType = null)
    {
        if (RegistrationMetadata.IsConsumerRegistrationExcluded(consumerType))
            throw new ArgumentException($"{TypeCache.GetShortName(consumerType)} is a saga, and cannot be registered as a consumer", nameof(consumerType));

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
