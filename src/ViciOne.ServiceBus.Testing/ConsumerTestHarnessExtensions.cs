using System;
using ViciOne.ServiceBus.Consumer;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for consumer test harness.
/// </summary>
public static class ConsumerTestHarnessExtensions
{
    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, string? queueName = null)
        where T : class, IConsumer, new()
    {
        var consumerFactory = new DefaultConstructorConsumerFactory<T>();

        return new ConsumerTestHarness<T>(harness, consumerFactory, queueName);
    }

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, Action<IConsumerConfigurator<T>> configure,
        string? queueName = null)
        where T : class, IConsumer, new()
    {
        var consumerFactory = new DefaultConstructorConsumerFactory<T>();

        return new ConsumerTestHarness<T>(harness, consumerFactory, configure, queueName);
    }

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, IConsumerFactory<T> consumerFactory, string? queueName = null)
        where T : class, IConsumer, new()
    {
        return new ConsumerTestHarness<T>(harness, consumerFactory, queueName);
    }

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, IConsumerFactory<T> consumerFactory,
        Action<IConsumerConfigurator<T>> configure, string? queueName = null)
        where T : class, IConsumer, new()
    {
        return new ConsumerTestHarness<T>(harness, consumerFactory, configure, queueName);
    }

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="consumerFactoryMethod">The consumer factory method value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, Func<T> consumerFactoryMethod, string? queueName = null)
        where T : class, IConsumer
    {
        return new ConsumerTestHarness<T>(harness, new DelegateConsumerFactory<T>(consumerFactoryMethod), queueName);
    }

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="consumerFactoryMethod">The consumer factory method value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConsumerTestHarness<T> Consumer<T>(this BusTestHarness harness, Func<T> consumerFactoryMethod,
        Action<IConsumerConfigurator<T>> configure, string? queueName = null)
        where T : class, IConsumer
    {
        return new ConsumerTestHarness<T>(harness, new DelegateConsumerFactory<T>(consumerFactoryMethod), configure, queueName);
    }
}
