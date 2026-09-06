using System;
using ViciOne.ServiceBus.Consumer;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for consumer test harness.</summary>
public static class ConsumerTestHarnessExtensions
{
    /// <summary>Creates a test harness for the selected consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The consumer test harness produced by the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, string? queueName = null)
        where T : class, IConsumer, new()
    {
        var consumerFactory = new DefaultConstructorConsumerFactory<T>();

        return new ConsumerTestHarness<T>(harness, consumerFactory, queueName);
    }

    /// <summary>Creates a test harness for the selected consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The consumer test harness produced by the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, Action<IConsumerConfigurator<T>> configure,
        string? queueName = null)
        where T : class, IConsumer, new()
    {
        var consumerFactory = new DefaultConstructorConsumerFactory<T>();

        return new ConsumerTestHarness<T>(harness, consumerFactory, configure, queueName);
    }

    /// <summary>Creates a test harness for the selected consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The consumer test harness produced by the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, IConsumerFactory<T> consumerFactory, string? queueName = null)
        where T : class, IConsumer, new()
    {
        return new ConsumerTestHarness<T>(harness, consumerFactory, queueName);
    }

    /// <summary>Creates a test harness for the selected consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The consumer test harness produced by the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, IConsumerFactory<T> consumerFactory,
        Action<IConsumerConfigurator<T>> configure, string? queueName = null)
        where T : class, IConsumer, new()
    {
        return new ConsumerTestHarness<T>(harness, consumerFactory, configure, queueName);
    }

    /// <summary>Creates a test harness for the selected consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="consumerFactoryMethod">The consumer factory method.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The consumer test harness produced by the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, Func<T> consumerFactoryMethod, string? queueName = null)
        where T : class, IConsumer
    {
        return new ConsumerTestHarness<T>(harness, new DelegateConsumerFactory<T>(consumerFactoryMethod), queueName);
    }

    /// <summary>Creates a test harness for the selected consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="consumerFactoryMethod">The consumer factory method.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The consumer test harness produced by the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, Func<T> consumerFactoryMethod,
        Action<IConsumerConfigurator<T>> configure, string? queueName = null)
        where T : class, IConsumer
    {
        return new ConsumerTestHarness<T>(harness, new DelegateConsumerFactory<T>(consumerFactoryMethod), configure, queueName);
    }
}
