using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Registers consumer factories with receive endpoints and consume pipes.</summary>
public static class ConsumerExtensions
{
    /// <summary>Registers a consumer factory with a receive endpoint.</summary>
    /// <typeparam name="TConsumer">The consumer implementation to register.</typeparam>
    /// <param name="configurator">The receive endpoint that will host the consumer.</param>
    /// <param name="consumerFactory">The factory that supplies a consumer for each delivery.</param>
    /// <param name="configure">An optional callback that configures the consumer pipeline.</param>
    public static void Consumer<TConsumer>(this IReceiveEndpointConfigurator configurator, IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerConfigurator<TConsumer>>? configure = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerFactory);

        try
        {
            LogContext.Debug?.Log("Subscribing Consumer: {ConsumerType} (using supplied consumer factory)", TypeCache<TConsumer>.ShortName);
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        var consumerConfigurator = new ConsumerConfigurator<TConsumer>(consumerFactory, configurator);

        configure?.Invoke(consumerConfigurator);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    /// <summary>Connects a consumer factory directly to a consume pipe.</summary>
    /// <typeparam name="TConsumer">The consumer implementation to connect.</typeparam>
    /// <param name="connector">The consume pipe that will dispatch messages to the consumer.</param>
    /// <param name="consumerFactory">The factory that supplies a consumer for each delivery.</param>
    /// <param name="pipeSpecifications">Middleware specifications applied around the consumer instance.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectConsumer<TConsumer>(this IConsumePipeConnector connector, IConsumerFactory<TConsumer> consumerFactory,
        params IPipeSpecification<ConsumerConsumeContext<TConsumer>>[] pipeSpecifications)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(pipeSpecifications);

        try
        {
            LogContext.Debug?.Log("Connecting Consumer: {ConsumerType} (using supplied consumer factory)", TypeCache<TConsumer>.ShortName);
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        IConsumerSpecification<TConsumer> specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();
        foreach (IPipeSpecification<ConsumerConsumeContext<TConsumer>> pipeSpecification in pipeSpecifications)
        {
            ArgumentNullException.ThrowIfNull(pipeSpecification);
            specification.AddPipeSpecification(pipeSpecification);
        }

        return ConsumerConnectorCache<TConsumer>.Connector.ConnectConsumer(connector, consumerFactory, specification);
    }

    /// <summary>Subscribes a consumer with a default constructor to the endpoint.</summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <param name="configurator">The receive endpoint that will host the consumer.</param>
    /// <param name="configure">An optional callback that configures the consumer pipeline.</param>
    public static void Consumer<TConsumer>(this IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<TConsumer>>? configure = null)
        where TConsumer : class, IConsumer, new()
    {
        ArgumentNullException.ThrowIfNull(configurator);

        try
        {
            LogContext.Debug?.Log("Subscribing Consumer: {ConsumerType} (using default constructor)", TypeCache<TConsumer>.ShortName);
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        var consumerFactory = new DefaultConstructorConsumerFactory<TConsumer>();

        var consumerConfigurator = new ConsumerConfigurator<TConsumer>(consumerFactory, configurator);

        configure?.Invoke(consumerConfigurator);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    /// <summary>Connects a default-constructed consumer directly to a consume pipe.</summary>
    /// <typeparam name="TConsumer">The default-constructible consumer implementation to connect.</typeparam>
    /// <param name="connector">The consume pipe that will dispatch messages to the consumer.</param>
    /// <param name="pipeSpecifications">Middleware specifications applied around the consumer instance.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectConsumer<TConsumer>(this IConsumePipeConnector connector,
        params IPipeSpecification<ConsumerConsumeContext<TConsumer>>[] pipeSpecifications)
        where TConsumer : class, IConsumer, new()
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(pipeSpecifications);

        try
        {
            LogContext.Debug?.Log("Connecting Consumer: {ConsumerType} (using default constructor)", TypeCache<TConsumer>.ShortName);
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        return ConnectConsumer(connector, new DefaultConstructorConsumerFactory<TConsumer>(), pipeSpecifications);
    }

    /// <summary>Registers a consumer created by the supplied factory delegate.</summary>
    /// <typeparam name="TConsumer">The consumer implementation to register.</typeparam>
    /// <param name="configurator">The receive endpoint that will host the consumer.</param>
    /// <param name="consumerFactoryMethod">The delegate that creates a consumer for each delivery.</param>
    /// <param name="configure">An optional callback that configures the consumer pipeline.</param>
    public static void Consumer<TConsumer>(this IReceiveEndpointConfigurator configurator, Func<TConsumer> consumerFactoryMethod,
        Action<IConsumerConfigurator<TConsumer>>? configure = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerFactoryMethod);

        try
        {
            LogContext.Debug?.Log("Subscribing Consumer: {ConsumerType} (using delegate consumer factory)", TypeCache<TConsumer>.ShortName);
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        var delegateConsumerFactory = new DelegateConsumerFactory<TConsumer>(consumerFactoryMethod);

        var consumerConfigurator = new ConsumerConfigurator<TConsumer>(delegateConsumerFactory, configurator);

        configure?.Invoke(consumerConfigurator);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    /// <summary>Connects a delegate-created consumer directly to a consume pipe.</summary>
    /// <typeparam name="TConsumer">The consumer implementation to connect.</typeparam>
    /// <param name="connector">The consume pipe that will dispatch messages to the consumer.</param>
    /// <param name="consumerFactoryMethod">The delegate that creates a consumer for each delivery.</param>
    /// <param name="pipeSpecifications">Middleware specifications applied around the consumer instance.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectConsumer<TConsumer>(this IConsumePipeConnector connector, Func<TConsumer> consumerFactoryMethod,
        params IPipeSpecification<ConsumerConsumeContext<TConsumer>>[] pipeSpecifications)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(consumerFactoryMethod);
        ArgumentNullException.ThrowIfNull(pipeSpecifications);

        try
        {
            LogContext.Debug?.Log("Connecting Consumer: {ConsumerType} (using delegate consumer factory)", TypeCache<TConsumer>.ShortName);
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        var consumerFactory = new DelegateConsumerFactory<TConsumer>(consumerFactoryMethod);

        return ConnectConsumer(connector, consumerFactory, pipeSpecifications);
    }

    /// <summary>Registers a runtime consumer type created by an object factory.</summary>
    /// <param name="configurator">The receive endpoint that will host the consumer.</param>
    /// <param name="consumerType">The closed reference type whose message contracts are discovered by registered conventions.</param>
    /// <param name="consumerFactory">The delegate that creates the requested runtime consumer type.</param>
    public static void Consumer(this IReceiveEndpointConfigurator configurator, Type consumerType, Func<Type, object> consumerFactory)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerType);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        EnsureClosedReferenceType(consumerType);

        try
        {
            LogContext.Debug?.Log("Subscribing Consumer: {ConsumerType} (by type, using object consumer factory)",
                TypeCache.GetShortName(consumerType));
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        var configuratorType = typeof(UntypedConsumerConfigurator<>).MakeGenericType(consumerType);
        var consumerConfigurator = (IReceiveEndpointSpecification)(Activator.CreateInstance(configuratorType, consumerFactory, configurator)
            ?? throw new InvalidOperationException("The requested runtime type could not be activated."));

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    /// <summary>Connects a runtime consumer type created by an object factory directly to a consume pipe.</summary>
    /// <param name="connector">The consume pipe that will dispatch messages to the consumer.</param>
    /// <param name="consumerType">The closed reference type whose message contracts are discovered by registered conventions.</param>
    /// <param name="objectFactory">The delegate that creates the requested runtime consumer type.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectConsumer(this IConsumePipeConnector connector, Type consumerType, Func<Type, object> objectFactory)
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(consumerType);
        ArgumentNullException.ThrowIfNull(objectFactory);
        EnsureClosedReferenceType(consumerType);

        try
        {
            LogContext.Debug?.Log("Connecting Consumer: {ConsumerType} (by type, using object consumer factory)", TypeCache.GetShortName(consumerType));
        }
        catch (Exception)
        {
            // Optional diagnostics do not prevent consumer registration.
        }

        return ConsumerConnectorCache.Connect(connector, consumerType, objectFactory);
    }

    static void EnsureClosedReferenceType(Type consumerType)
    {
        if (consumerType.IsValueType || consumerType.IsByRef || consumerType.IsPointer || consumerType.ContainsGenericParameters)
            throw new ArgumentException("The consumer type must be a closed reference type.", nameof(consumerType));
    }
}
