using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an endpoint configuration implementation.
/// </summary>
public class EndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topology">The topology value.</param>
    protected EndpointConfiguration(ITopologyConfiguration topology)
    {
        Topology = topology;

        Consume = new ConsumePipeConfiguration(topology.Consume);
        Send = new SendPipeConfiguration(topology.Send);
        Publish = new PublishPipeConfiguration(topology.Publish);
        Receive = new ReceivePipeConfiguration();

        Serialization = new SerializationConfiguration();
        Transport = new TransportConfiguration();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="parentConfiguration">The parent configuration value.</param>
    /// <param name="topology">The topology value.</param>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    protected EndpointConfiguration(IEndpointConfiguration parentConfiguration, ITopologyConfiguration topology, bool isBusEndpoint)
    {
        Topology = topology;

        Consume = new ConsumePipeConfiguration(parentConfiguration.Consume.Specification);
        Send = new SendPipeConfiguration(parentConfiguration.Send.Specification);
        Publish = new PublishPipeConfiguration(parentConfiguration.Publish.Specification);
        Receive = new ReceivePipeConfiguration();

        Serialization = parentConfiguration.Serialization.CreateSerializationConfiguration();

        Transport = new TransportConfiguration(parentConfiguration.Transport);

        IsBusEndpoint = parentConfiguration.IsBusEndpoint || isBusEndpoint;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    protected EndpointConfiguration(IEndpointConfiguration endpointConfiguration)
    {
        Topology = endpointConfiguration.Topology;

        Consume = endpointConfiguration.Consume;
        Send = endpointConfiguration.Send;
        Publish = endpointConfiguration.Publish;
        Receive = endpointConfiguration.Receive;

        Serialization = endpointConfiguration.Serialization;

        Transport = endpointConfiguration.Transport;

        IsBusEndpoint = endpointConfiguration.IsBusEndpoint;
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        get => Transport.ConcurrentMessageLimit;
        set => Transport.Configurator.ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public int PrefetchCount
    {
        get => Transport.PrefetchCount;
        set => Transport.Configurator.PrefetchCount = value;
    }

    /// <summary>
    /// Gets or sets the default content type value.
    /// </summary>
    public ContentType DefaultContentType
    {
        set => Serialization.DefaultContentType = value;
    }

    /// <summary>
    /// Gets or sets the serializer content type value.
    /// </summary>
    public ContentType SerializerContentType
    {
        set => Serialization.SerializerContentType = value;
    }

    /// <summary>
    /// Gets the is bus endpoint value.
    /// </summary>
    public bool IsBusEndpoint { get; }

    /// <summary>
    /// Gets or sets the auto start value.
    /// </summary>
    public bool AutoStart
    {
        set => Consume.Configurator.AutoStart = value;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
        where T : class
    {
        Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds pre pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        Consume.Configurator.AddPrePipeSpecification(specification);
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectConsumerConfigurationObserver(observer);
    }

    void IConsumerConfigurationObserver.ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
    {
        Consume.Configurator.ConsumerConfigured(configurator);
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
    {
        Consume.Configurator.ConsumerMessageConfigured(configurator);
    }

    /// <summary>
    /// Connects saga configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Performs the saga configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        Consume.Configurator.SagaConfigured(configurator);
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
        Consume.Configurator.StateMachineSagaConfigured(configurator, stateMachine);
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
        Consume.Configurator.SagaMessageConfigured(configurator);
    }

    /// <summary>
    /// Connects handler configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Performs the handler configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        Consume.Configurator.HandlerConfigured(configurator);
    }

    /// <summary>
    /// Connects activity configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        Consume.Configurator.ActivityConfigured(configurator, compensateAddress);
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
        Consume.Configurator.ExecuteActivityConfigured(configurator);
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
        Consume.Configurator.CompensateActivityConfigured(configurator);
    }

    /// <summary>
    /// Configures publish.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigurePublish(Action<IPublishPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Publish.Configurator);
    }

    /// <summary>
    /// Configures send.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Send.Configurator);
    }

    /// <summary>
    /// Configures receive.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureReceive(Action<IReceivePipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Receive.Configurator);
    }

    /// <summary>
    /// Configures dead letter.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureDeadLetter(Action<IPipeConfigurator<ReceiveContext>> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Receive.DeadLetterConfigurator);
    }

    /// <summary>
    /// Configures error.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureError(Action<IPipeConfigurator<ExceptionReceiveContext>> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Receive.ErrorConfigurator);
    }

    /// <summary>
    /// Configures transport.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void ConfigureTransport(Action<ITransportConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Transport.Configurator);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return Send.Specification.Validate()
            .Concat(Publish.Specification.Validate())
            .Concat(Consume.Specification.Validate())
            .Concat(Receive.Specification.Validate())
            .Concat(Topology.Validate())
            .Concat(Serialization.Validate())
            .Concat(Transport.Validate());
    }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    public IConsumePipeConfiguration Consume { get; }
    /// <summary>
    /// Gets the send value.
    /// </summary>
    public ISendPipeConfiguration Send { get; }
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    public IPublishPipeConfiguration Publish { get; }
    /// <summary>
    /// Gets the receive value.
    /// </summary>
    public IReceivePipeConfiguration Receive { get; }
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public ITopologyConfiguration Topology { get; }
    /// <summary>
    /// Gets the serialization value.
    /// </summary>
    public ISerializationConfiguration Serialization { get; }
    /// <summary>
    /// Gets the transport value.
    /// </summary>
    public ITransportConfiguration Transport { get; }

    /// <summary>
    /// Adds serializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isSerializer">The is serializer value.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        Serialization.AddSerializer(factory, isSerializer);
    }

    /// <summary>
    /// Adds deserializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isDefault">The is default value.</param>
    public void AddDeserializer(ISerializerFactory factory, bool isDefault = false)
    {
        Serialization.AddDeserializer(factory, isDefault);
    }

    /// <summary>
    /// Configures system text json serializer options.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
    {
        Serialization.ConfigureSystemTextJsonSerializerOptions(configure);
    }

    /// <summary>
    /// Performs the clear serialization operation.
    /// </summary>
    public void ClearSerialization()
    {
        Serialization.Clear();
    }
}
