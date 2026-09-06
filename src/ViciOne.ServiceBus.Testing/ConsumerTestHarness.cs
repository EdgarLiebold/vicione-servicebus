using System;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides a test harness for consumer test.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerTestHarness<TConsumer> :
    IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly Action<IConsumerConfigurator<TConsumer>>? _configure;
    readonly ReceivedMessageList _consumed;
    readonly IConsumerFactory<TConsumer> _consumerFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="queueName">The queue name.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerConfigurator<TConsumer>> configure, string? queueName)
        : this(testHarness, consumerFactory, queueName)
    {
        _configure = configure;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="queueName">The queue name.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory, string? queueName)
        : this(testHarness, consumerFactory)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            testHarness.OnConfigureReceiveEndpoint += ConfigureReceiveEndpoint;
        else
            testHarness.OnConfigureBus += configurator => ConfigureNamedReceiveEndpoint(configurator, queueName);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerConfigurator<TConsumer>> configure)
        : this(testHarness, consumerFactory)
    {
        _configure = configure;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory)
    {
        _consumerFactory = consumerFactory;

        _consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>Gets the consumed.</summary>
    public IReceivedMessageList Consumed => _consumed;

    /// <summary>Configures receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected virtual void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        var decorator = new TestConsumerFactoryDecorator<TConsumer>(_consumerFactory, _consumed);

        configurator.Consumer(decorator, c => _configure?.Invoke(c));
    }

    /// <summary>Configures named receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="queueName">The queue name.</param>
    protected virtual void ConfigureNamedReceiveEndpoint(IBusFactoryConfigurator configurator, string queueName)
    {
        configurator.ReceiveEndpoint(queueName, x =>
        {
            var decorator = new TestConsumerFactoryDecorator<TConsumer>(_consumerFactory, _consumed);

            x.Consumer(decorator, c => _configure?.Invoke(c));
        });
    }
}
