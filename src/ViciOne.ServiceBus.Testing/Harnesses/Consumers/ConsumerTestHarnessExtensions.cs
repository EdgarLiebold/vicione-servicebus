using System;
using ViciOne.ServiceBus.Consumer;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates consumer-specific observers for a bus test harness.</summary>
public static class ConsumerTestHarnessExtensions
{
    /// <summary>Registers a default-constructed consumer with the harness.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="harness">The harness that hosts the consumer.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>A harness that records deliveries to the consumer.</returns>
    public static ConsumerTestHarness<TConsumer> Consumer<TConsumer>(this BusTestHarness harness, string? queueName = null)
        where TConsumer : class, IConsumer, new()
    {
        ArgumentNullException.ThrowIfNull(harness);
        var consumerFactory = new DefaultConstructorConsumerFactory<TConsumer>();

        return new ConsumerTestHarness<TConsumer>(harness, consumerFactory, queueName);
    }

    /// <summary>Registers and configures a default-constructed consumer with the harness.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="harness">The harness that hosts the consumer.</param>
    /// <param name="configure">The consumer configuration callback.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>A harness that records deliveries to the consumer.</returns>
    public static ConsumerTestHarness<TConsumer> Consumer<TConsumer>(this BusTestHarness harness,
        Action<IConsumerConfigurator<TConsumer>> configure,
        string? queueName = null)
        where TConsumer : class, IConsumer, new()
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(configure);
        var consumerFactory = new DefaultConstructorConsumerFactory<TConsumer>();

        return new ConsumerTestHarness<TConsumer>(harness, consumerFactory, configure, queueName);
    }

    /// <summary>Registers a factory-created consumer with the harness.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="harness">The harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The factory that creates consumer instances.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>A harness that records deliveries to the consumer.</returns>
    public static ConsumerTestHarness<TConsumer> Consumer<TConsumer>(this BusTestHarness harness,
        IConsumerFactory<TConsumer> consumerFactory, string? queueName = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        return new ConsumerTestHarness<TConsumer>(harness, consumerFactory, queueName);
    }

    /// <summary>Registers and configures a factory-created consumer with the harness.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="harness">The harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The factory that creates consumer instances.</param>
    /// <param name="configure">The consumer configuration callback.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>A harness that records deliveries to the consumer.</returns>
    public static ConsumerTestHarness<TConsumer> Consumer<TConsumer>(this BusTestHarness harness,
        IConsumerFactory<TConsumer> consumerFactory, Action<IConsumerConfigurator<TConsumer>> configure, string? queueName = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(configure);
        return new ConsumerTestHarness<TConsumer>(harness, consumerFactory, configure, queueName);
    }

    /// <summary>Registers a delegate-created consumer with the harness.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="harness">The harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The delegate that creates consumer instances.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>A harness that records deliveries to the consumer.</returns>
    public static ConsumerTestHarness<TConsumer> Consumer<TConsumer>(this BusTestHarness harness, Func<TConsumer> consumerFactory,
        string? queueName = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        return new ConsumerTestHarness<TConsumer>(harness, new DelegateConsumerFactory<TConsumer>(consumerFactory), queueName);
    }

    /// <summary>Registers and configures a delegate-created consumer with the harness.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="harness">The harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The delegate that creates consumer instances.</param>
    /// <param name="configure">The consumer configuration callback.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>A harness that records deliveries to the consumer.</returns>
    public static ConsumerTestHarness<TConsumer> Consumer<TConsumer>(this BusTestHarness harness, Func<TConsumer> consumerFactory,
        Action<IConsumerConfigurator<TConsumer>> configure, string? queueName = null)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(configure);
        return new ConsumerTestHarness<TConsumer>(harness, new DelegateConsumerFactory<TConsumer>(consumerFactory), configure, queueName);
    }
}
