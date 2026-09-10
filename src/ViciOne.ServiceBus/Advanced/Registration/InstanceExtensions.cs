using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Registers existing consumer instances with receive endpoints and consume pipes.</summary>
public static class InstanceExtensions
{
    /// <summary>Adds every consumer contract implemented by an object instance to a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="instance">The consumer instance to register.</param>
    public static void Instance(this IReceiveEndpointConfigurator configurator, object instance)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(instance);

        var instanceConfigurator = new InstanceConfigurator(instance);

        configurator.AddEndpointSpecification(instanceConfigurator);
    }

    /// <summary>Connects every consumer contract implemented by an object instance to a consume pipe.</summary>
    /// <param name="connector">The consume pipe to extend.</param>
    /// <param name="instance">The consumer instance to connect.</param>
    /// <returns>A handle that disconnects all contracts implemented by the instance.</returns>
    public static ConnectHandle ConnectInstance(this IConsumePipeConnector connector, object instance)
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(instance);

        return InstanceConnectorCache.GetInstanceConnector(instance.GetType()).ConnectInstance(connector, instance);
    }

    /// <summary>Adds a strongly typed consumer instance to a receive endpoint.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="instance">The consumer instance to register.</param>
    /// <param name="configure">An optional callback that configures the instance pipeline.</param>
    public static void Instance<TConsumer>(this IReceiveEndpointConfigurator configurator, TConsumer instance,
        Action<IInstanceConfigurator<TConsumer>>? configure = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(instance);

        var instanceConfigurator = new InstanceConfigurator<TConsumer>(instance, configurator);

        configure?.Invoke(instanceConfigurator);

        configurator.AddEndpointSpecification(instanceConfigurator);
    }

    /// <summary>Connects a strongly typed consumer instance to a consume pipe.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="connector">The consume pipe to extend.</param>
    /// <param name="instance">The consumer instance to connect.</param>
    /// <returns>A handle that disconnects every contract implemented by the instance.</returns>
    public static ConnectHandle ConnectInstance<TConsumer>(this IConsumePipeConnector connector, TConsumer instance)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(instance);

        return InstanceConnectorCache<TConsumer>.Connector.ConnectInstance(connector, instance);
    }
}
