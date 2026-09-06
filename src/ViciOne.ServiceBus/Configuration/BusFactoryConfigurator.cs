using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Coordinates transport-independent bus, topology, serialization, and pipeline configuration.</summary>
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

    /// <summary>Initializes a configurator backed by the supplied bus configuration.</summary>
    /// <param name="busConfiguration">The mutable configuration assembled for the bus.</param>
    protected BusFactoryConfigurator(IBusConfiguration busConfiguration)
    {
        _busConfiguration = busConfiguration;

        busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = false;

        if (LogContext.Current == null)
            LogContext.ConfigureCurrentLogContext();
    }

    /// <summary>Gets the conventions that describe message contracts.</summary>
    public IMessageTopologyConfigurator MessageTopology => _busConfiguration.Topology.Message;
    /// <summary>Gets the topology applied when messages are consumed.</summary>
    public IConsumeTopologyConfigurator ConsumeTopology => _busConfiguration.Topology.Consume;
    /// <summary>Gets the topology applied when messages are sent.</summary>
    public ISendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>Gets the topology applied when messages are published.</summary>
    public IPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Sets whether startup deploys topology without starting message delivery.</summary>
    public bool DeployTopologyOnly
    {
        set => _busConfiguration.HostConfiguration.DeployTopologyOnly = value;
    }

    /// <summary>Sets whether startup deploys publish topology for configured message contracts.</summary>
    public bool DeployPublishTopology
    {
        set => _busConfiguration.HostConfiguration.DeployPublishTopology = value;
    }

    /// <summary>Sets the maximum number of messages processed concurrently.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _busConfiguration.Transport.Configurator.ConcurrentMessageLimit = value;
    }

    /// <summary>Sets the maximum number of messages prefetched from the transport.</summary>
    public int PrefetchCount
    {
        set => _busConfiguration.Transport.Configurator.PrefetchCount = value;
    }

    /// <summary>Sets the content type used to deserialize messages that do not declare one.</summary>
    public ContentType DefaultContentType
    {
        set => _busConfiguration.Serialization.DefaultContentType = value;
    }

    /// <summary>Sets the content type used to serialize outgoing messages.</summary>
    public ContentType SerializerContentType
    {
        set => _busConfiguration.Serialization.SerializerContentType = value;
    }

    /// <summary>Subscribes an observer to bus lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives bus lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busConfiguration.ConnectBusObserver(observer);
    }

    /// <summary>Subscribes an observer to consume pipeline notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectConsumeObserver(observer);
    }

    /// <summary>Sets whether the bus endpoint starts with its host.</summary>
    public virtual bool AutoStart
    {
        set => _busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = value;
    }

    /// <summary>Adds a specification to the consume pipeline.</summary>
    /// <param name="specification">The specification that configures the consume pipeline.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        _busConfiguration.Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds a specification before the main consume pipeline.</summary>
    /// <param name="specification">The specification that configures the pre-consume pipeline.</param>
    public void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        _busConfiguration.Consume.Configurator.AddPrePipeSpecification(specification);
    }

    /// <summary>Adds a specification to the consume pipeline for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="specification">The specification that configures the typed consume pipeline.</param>
    public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
        where T : class
    {
        _busConfiguration.Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>Subscribes an observer to consumer configuration notifications.</summary>
    /// <param name="observer">The observer that receives consumer configuration notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _busConfiguration.Consume.Configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Subscribes an observer to saga configuration notifications.</summary>
    /// <param name="observer">The observer that receives saga configuration notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return _busConfiguration.Consume.Configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>Subscribes an observer to message-handler configuration notifications.</summary>
    /// <param name="observer">The observer that receives handler configuration notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return _busConfiguration.Consume.Configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>Subscribes an observer to routing-slip activity configuration notifications.</summary>
    /// <param name="observer">The observer that receives activity configuration notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer)
    {
        return _busConfiguration.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>Notifies registered observers that a consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The configured consumer type.</typeparam>
    /// <param name="configurator">The consumer configuration reported to observers.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        _busConfiguration.Consume.Configurator.ConsumerConfigured(configurator);
    }

    /// <summary>Notifies registered observers that a consumer message has been configured.</summary>
    /// <typeparam name="TConsumer">The configured consumer type.</typeparam>
    /// <typeparam name="TMessage">The message contract consumed by the consumer.</typeparam>
    /// <param name="configurator">The message configuration reported to observers.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        _busConfiguration.Consume.Configurator.ConsumerMessageConfigured(configurator);
    }

    /// <summary>Notifies registered observers that a saga has been configured.</summary>
    /// <typeparam name="TSaga">The configured saga state type.</typeparam>
    /// <param name="configurator">The saga configuration reported to observers.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        _busConfiguration.Consume.Configurator.SagaConfigured(configurator);
    }

    /// <summary>Notifies registered observers that a state-machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The state-machine instance type.</typeparam>
    /// <param name="configurator">The saga configuration reported to observers.</param>
    /// <param name="stateMachine">The configured state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        _busConfiguration.Consume.Configurator.StateMachineSagaConfigured(configurator, stateMachine);
    }

    /// <summary>Notifies registered observers that a saga message has been configured.</summary>
    /// <typeparam name="TSaga">The configured saga state type.</typeparam>
    /// <typeparam name="TMessage">The message contract correlated to the saga.</typeparam>
    /// <param name="configurator">The saga-message configuration reported to observers.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        _busConfiguration.Consume.Configurator.SagaMessageConfigured(configurator);
    }

    /// <summary>Notifies registered observers that a message handler has been configured.</summary>
    /// <typeparam name="TMessage">The message contract handled by the pipeline.</typeparam>
    /// <param name="configurator">The handler configuration reported to observers.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        _busConfiguration.Consume.Configurator.HandlerConfigured(configurator);
    }

    /// <summary>Notifies registered observers that a routing-slip activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity implementation type.</typeparam>
    /// <typeparam name="TArguments">The activity argument contract.</typeparam>
    /// <param name="configurator">The execution configuration reported to observers.</param>
    /// <param name="compensateAddress">The destination that handles compensation.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        _busConfiguration.Consume.Configurator.ActivityConfigured(configurator, compensateAddress);
    }

    /// <summary>Notifies registered observers that activity execution has been configured.</summary>
    /// <typeparam name="TActivity">The activity implementation type.</typeparam>
    /// <typeparam name="TArguments">The activity argument contract.</typeparam>
    /// <param name="configurator">The execution configuration reported to observers.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        _busConfiguration.Consume.Configurator.ExecuteActivityConfigured(configurator);
    }

    /// <summary>Notifies registered observers that activity compensation has been configured.</summary>
    /// <typeparam name="TActivity">The activity implementation type.</typeparam>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="configurator">The compensation configuration reported to observers.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        _busConfiguration.Consume.Configurator.CompensateActivityConfigured(configurator);
    }

    /// <summary>Subscribes an observer to receive-endpoint configuration notifications.</summary>
    /// <param name="observer">The observer that receives endpoint configuration notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _busConfiguration.ConnectEndpointConfigurationObserver(observer);
    }

    /// <summary>Subscribes an observer to publish pipeline notifications.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectPublishObserver(observer);
    }

    /// <summary>Applies configuration to the bus publish pipeline.</summary>
    /// <param name="callback">The callback that configures the publish pipeline.</param>
    public void ConfigurePublish(Action<IPublishPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(_busConfiguration.Publish.Configurator);
    }

    /// <summary>Subscribes an observer to receive transport notifications.</summary>
    /// <param name="observer">The observer that receives transport notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes an observer to send pipeline notifications.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _busConfiguration.HostConfiguration.ConnectSendObserver(observer);
    }

    /// <summary>Applies configuration to the bus send pipeline.</summary>
    /// <param name="callback">The callback that configures the send pipeline.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(_busConfiguration.Send.Configurator);
    }

    /// <summary>Configures message-topology conventions for a contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">The callback that configures the contract topology.</param>
    public void Message<T>(Action<IMessageTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IMessageTopologyConfigurator<T> configurator = _busConfiguration.Topology.Message.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">The callback that configures send topology.</param>
    public void Send<T>(Action<IMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">The callback that configures publish topology.</param>
    public void Publish<T>(Action<IMessagePublishTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Maps a message contract to a fixed destination on this bus.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="destinationAddress">The destination to use for the contract.</param>
    public void Route<T>(Uri destinationAddress)
        where T : class
    {
        ((MessageRouteTable)_busConfiguration.MessageRoutes).Map<T>(destinationAddress);
    }

    /// <summary>Maps a message contract to a destination resolved for each send on this bus.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpointAddressProvider">The function that resolves the current destination, or <see langword="null" /> when none is available.</param>
    public void Route<T>(EndpointAddressProvider endpointAddressProvider)
        where T : class
    {
        ((MessageRouteTable)_busConfiguration.MessageRoutes).Map<T>(endpointAddressProvider);
    }

    /// <summary>Validates bus, host, and bus-endpoint configuration.</summary>
    /// <returns>All validation failures found across the configuration graph.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _busConfiguration.Validate()
            .Concat(_busConfiguration.HostConfiguration.Validate())
            .Concat(_busConfiguration.BusEndpointConfiguration.Validate());
    }

    /// <summary>Adds a message serializer to the bus.</summary>
    /// <param name="factory">The factory that creates serializer contexts.</param>
    /// <param name="isSerializer">Whether this serializer becomes the active serializer.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        _busConfiguration.Serialization.AddSerializer(factory, isSerializer);
    }

    /// <summary>Adds a message deserializer to the bus.</summary>
    /// <param name="factory">The factory that creates deserializer contexts.</param>
    /// <param name="isDefault">Whether the deserializer's content type becomes the default.</param>
    public void AddDeserializer(ISerializerFactory factory, bool isDefault = false)
    {
        _busConfiguration.Serialization.AddDeserializer(factory, isDefault);
    }

    /// <summary>Configures the immutable <see cref="JsonSerializerOptions" /> snapshot used by this bus.</summary>
    /// <param name="configure">The function that transforms the serializer options.</param>
    public void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
    {
        _busConfiguration.Serialization.ConfigureSystemTextJsonSerializerOptions(configure);
    }

    /// <summary>Removes every configured serializer and deserializer from the bus.</summary>
    public void ClearSerialization()
    {
        _busConfiguration.Serialization.Clear();
    }
}
