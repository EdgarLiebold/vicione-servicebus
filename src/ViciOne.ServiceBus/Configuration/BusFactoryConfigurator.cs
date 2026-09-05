using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a bus factory configurator implementation.
/// </summary>
public abstract class BusFactoryConfigurator :
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IEndpointConfigurationObserverConnector,
    IBusObserverConnector,
    IReceiveObserverConnector,
    IConsumeObserverConnector,
    ISendObserverConnector,
    IPublishObserverConnector
{
    readonly IBusConfiguration _busConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    protected BusFactoryConfigurator(IBusConfiguration busConfiguration)
    {
        _busConfiguration = busConfiguration;

        busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = false;

        if (LogContext.Current == null)
            LogContext.ConfigureCurrentLogContext();
    }

    /// <summary>
    /// Gets the message topology value.
    /// </summary>
    public IMessageTopologyConfigurator MessageTopology => _busConfiguration.Topology.Message;
    /// <summary>
    /// Gets the consume topology value.
    /// </summary>
    public IConsumeTopologyConfigurator ConsumeTopology => _busConfiguration.Topology.Consume;
    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public ISendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public IPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Gets or sets the deploy topology only value.
    /// </summary>
    public bool DeployTopologyOnly
    {
        set => _busConfiguration.HostConfiguration.DeployTopologyOnly = value;
    }

    /// <summary>
    /// Gets or sets the deploy publish topology value.
    /// </summary>
    public bool DeployPublishTopology
    {
        set => _busConfiguration.HostConfiguration.DeployPublishTopology = value;
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        set => _busConfiguration.Transport.Configurator.ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public int PrefetchCount
    {
        set => _busConfiguration.Transport.Configurator.PrefetchCount = value;
    }

    /// <summary>
    /// Gets or sets the default content type value.
    /// </summary>
    public ContentType DefaultContentType
    {
        set => _busConfiguration.Serialization.DefaultContentType = value;
    }

    /// <summary>
    /// Gets or sets the serializer content type value.
    /// </summary>
    public ContentType SerializerContentType
    {
        set => _busConfiguration.Serialization.SerializerContentType = value;
    }

    /// <summary>
    /// Connects bus observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busConfiguration.ConnectBusObserver(observer);
    }

    /// <summary>
    /// Connects consume observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectConsumeObserver(observer);
    }

    /// <summary>
    /// Gets or sets the auto start value.
    /// </summary>
    public virtual bool AutoStart
    {
        set => _busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = value;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        _busConfiguration.Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds pre pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        _busConfiguration.Consume.Configurator.AddPrePipeSpecification(specification);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
        where T : class
    {
        _busConfiguration.Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _busConfiguration.Consume.Configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Connects saga configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return _busConfiguration.Consume.Configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Connects handler configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return _busConfiguration.Consume.Configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Connects activity configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer)
    {
        return _busConfiguration.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>
    /// Consumes r configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        _busConfiguration.Consume.Configurator.ConsumerConfigured(configurator);
    }

    /// <summary>
    /// Consumes r message configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        _busConfiguration.Consume.Configurator.ConsumerMessageConfigured(configurator);
    }

    /// <summary>
    /// Performs the saga configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        _busConfiguration.Consume.Configurator.SagaConfigured(configurator);
    }

    /// <summary>
    /// Performs the state machine saga configured operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        _busConfiguration.Consume.Configurator.StateMachineSagaConfigured(configurator, stateMachine);
    }

    /// <summary>
    /// Performs the saga message configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        _busConfiguration.Consume.Configurator.SagaMessageConfigured(configurator);
    }

    /// <summary>
    /// Performs the handler configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        _busConfiguration.Consume.Configurator.HandlerConfigured(configurator);
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        _busConfiguration.Consume.Configurator.ActivityConfigured(configurator, compensateAddress);
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        _busConfiguration.Consume.Configurator.ExecuteActivityConfigured(configurator);
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        _busConfiguration.Consume.Configurator.CompensateActivityConfigured(configurator);
    }

    /// <summary>
    /// Connects endpoint configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _busConfiguration.ConnectEndpointConfigurationObserver(observer);
    }

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectPublishObserver(observer);
    }

    /// <summary>
    /// Configures publish.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigurePublish(Action<IPublishPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(_busConfiguration.Publish.Configurator);
    }

    /// <summary>
    /// Connects receive observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectReceiveObserver(observer);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Configures send.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(_busConfiguration.Send.Configurator);
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Message<T>(Action<IMessageTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IMessageTopologyConfigurator<T> configurator = _busConfiguration.Topology.Message.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Send<T>(Action<IMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Publish<T>(Action<IMessagePublishTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the route operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    public void Route<T>(Uri destinationAddress)
        where T : class
    {
        ((MessageRouteTable)_busConfiguration.MessageRoutes).Map<T>(destinationAddress);
    }

    /// <summary>
    /// Performs the route operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="endpointAddressProvider">The endpoint address provider value.</param>
    public void Route<T>(EndpointAddressProvider<T> endpointAddressProvider)
        where T : class
    {
        ((MessageRouteTable)_busConfiguration.MessageRoutes).Map(endpointAddressProvider);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _busConfiguration.Validate()
            .Concat(_busConfiguration.HostConfiguration.Validate())
            .Concat(_busConfiguration.BusEndpointConfiguration.Validate());
    }

    /// <summary>
    /// Adds serializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isSerializer">The is serializer value.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        _busConfiguration.Serialization.AddSerializer(factory, isSerializer);
    }

    /// <summary>
    /// Adds deserializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isDefault">The is default value.</param>
    public void AddDeserializer(ISerializerFactory factory, bool isDefault = false)
    {
        _busConfiguration.Serialization.AddDeserializer(factory, isDefault);
    }

    /// <summary>
    /// Configures system text json serializer options.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
    {
        _busConfiguration.Serialization.ConfigureSystemTextJsonSerializerOptions(configure);
    }

    /// <summary>
    /// Performs the clear serialization operation.
    /// </summary>
    public void ClearSerialization()
    {
        _busConfiguration.Serialization.Clear();
    }
}
