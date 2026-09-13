using System;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Registers a consumer under test and records the messages delivered to it.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
public class ConsumerTestHarness<TConsumer> :
    IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    readonly Action<IConsumerConfigurator<TConsumer>>? _configure;
    readonly ConsumedMessageList _consumed;
    readonly IConsumerFactory<TConsumer> _consumerFactory;

    /// <summary>Registers a configured consumer with either the default endpoint or a named endpoint.</summary>
    /// <param name="testHarness">The bus harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The factory that creates consumer instances.</param>
    /// <param name="configure">Configuration applied to the consumer.</param>
    /// <param name="queueName">The named endpoint queue, or <see langword="null"/> for the default endpoint.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerConfigurator<TConsumer>> configure, string? queueName)
        : this(testHarness, consumerFactory, queueName)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configure = configure;
    }

    /// <summary>Registers a consumer with either the default endpoint or a named endpoint.</summary>
    /// <param name="testHarness">The bus harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The factory that creates consumer instances.</param>
    /// <param name="queueName">The named endpoint queue, or <see langword="null"/> for the default endpoint.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory, string? queueName)
        : this(testHarness, consumerFactory)
    {
        if (queueName == null)
            testHarness.ReceiveEndpointConfiguring += ConfigureReceiveEndpoint;
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            testHarness.BusConfiguring += configurator => ConfigureNamedReceiveEndpoint(configurator, queueName);
        }
    }

    /// <summary>Registers a configured consumer with the default receive endpoint.</summary>
    /// <param name="testHarness">The bus harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The factory that creates consumer instances.</param>
    /// <param name="configure">Configuration applied to the consumer.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerConfigurator<TConsumer>> configure)
        : this(testHarness, consumerFactory)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configure = configure;
        testHarness.ReceiveEndpointConfiguring += ConfigureReceiveEndpoint;
    }

    /// <summary>Creates the consumer observer used by the harness.</summary>
    /// <param name="testHarness">The bus harness that hosts the consumer.</param>
    /// <param name="consumerFactory">The factory that creates consumer instances.</param>
    public ConsumerTestHarness(BusTestHarness testHarness, IConsumerFactory<TConsumer> consumerFactory)
    {
        ArgumentNullException.ThrowIfNull(testHarness);
        ArgumentNullException.ThrowIfNull(consumerFactory);

        _consumerFactory = consumerFactory;

        _consumed = new ConsumedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>Gets messages delivered to the consumer.</summary>
    public IConsumedMessageList Consumed => _consumed;

    /// <summary>Attaches the consumer observer to the default receive endpoint.</summary>
    /// <param name="configurator">The receive-endpoint configurator.</param>
    protected virtual void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        var decorator = new TestConsumerFactoryDecorator<TConsumer>(_consumerFactory, _consumed);

        configurator.Consumer(decorator, c => _configure?.Invoke(c));
    }

    /// <summary>Adds a named receive endpoint containing the observed consumer.</summary>
    /// <param name="configurator">The bus configurator.</param>
    /// <param name="queueName">The endpoint queue name.</param>
    protected virtual void ConfigureNamedReceiveEndpoint(IBusFactoryConfigurator configurator, string queueName)
    {
        configurator.ReceiveEndpoint(queueName, x =>
        {
            var decorator = new TestConsumerFactoryDecorator<TConsumer>(_consumerFactory, _consumed);

            x.Consumer(decorator, c => _configure?.Invoke(c));
        });
    }
}
