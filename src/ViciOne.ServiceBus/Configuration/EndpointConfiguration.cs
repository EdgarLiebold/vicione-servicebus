using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates endpoint configuration.</summary>
public class EndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topology">The topology.</param>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="parentConfiguration">The parent configuration.</param>
    /// <param name="topology">The topology.</param>
    /// <param name="isBusEndpoint">The is bus endpoint.</param>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
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

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit
    {
        get => Transport.ConcurrentMessageLimit;
        set => Transport.Configurator.ConcurrentMessageLimit = value;
    }

    /// <summary>Gets or sets the prefetch count.</summary>
    public int PrefetchCount
    {
        get => Transport.PrefetchCount;
        set => Transport.Configurator.PrefetchCount = value;
    }

    /// <summary>Sets the default content type.</summary>
    public ContentType DefaultContentType
    {
        set => Serialization.DefaultContentType = value;
    }

    /// <summary>Sets the serializer content type.</summary>
    public ContentType SerializerContentType
    {
        set => Serialization.SerializerContentType = value;
    }

    /// <summary>Gets a value indicating whether bus endpoint.</summary>
    public bool IsBusEndpoint { get; }

    /// <summary>Sets whether the endpoint starts automatically.</summary>
    public bool AutoStart
    {
        set => Consume.Configurator.AutoStart = value;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
        where T : class
    {
        Consume.Configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds pre pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        Consume.Configurator.AddPrePipeSpecification(specification);
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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

    /// <summary>Connects saga configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>Reports that saga has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        Consume.Configurator.SagaConfigured(configurator);
    }

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        Consume.Configurator.StateMachineSagaConfigured(configurator, stateMachine);
    }

    /// <summary>Reports that saga message has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        Consume.Configurator.SagaMessageConfigured(configurator);
    }

    /// <summary>Connects handler configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>Reports that handler has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        Consume.Configurator.HandlerConfigured(configurator);
    }

    /// <summary>Connects activity configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer)
    {
        return Consume.Configurator.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        Consume.Configurator.ActivityConfigured(configurator, compensateAddress);
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        Consume.Configurator.ExecuteActivityConfigured(configurator);
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        Consume.Configurator.CompensateActivityConfigured(configurator);
    }

    /// <summary>Configures publish.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ConfigurePublish(Action<IPublishPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Publish.Configurator);
    }

    /// <summary>Configures send.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ConfigureSend(Action<ISendPipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Send.Configurator);
    }

    /// <summary>Configures receive.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ConfigureReceive(Action<IReceivePipeConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Receive.Configurator);
    }

    /// <summary>Configures dead letter.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ConfigureDeadLetter(Action<IPipeConfigurator<ReceiveContext>> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Receive.DeadLetterConfigurator);
    }

    /// <summary>Configures error.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ConfigureError(Action<IPipeConfigurator<ExceptionReceiveContext>> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Receive.ErrorConfigurator);
    }

    /// <summary>Configures transport.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void ConfigureTransport(Action<ITransportConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        callback(Transport.Configurator);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
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

    /// <summary>Gets the consume.</summary>
    public IConsumePipeConfiguration Consume { get; }
    /// <summary>Gets the send.</summary>
    public ISendPipeConfiguration Send { get; }
    /// <summary>Gets the publish.</summary>
    public IPublishPipeConfiguration Publish { get; }
    /// <summary>Gets the receive.</summary>
    public IReceivePipeConfiguration Receive { get; }
    /// <summary>Gets the topology.</summary>
    public ITopologyConfiguration Topology { get; }
    /// <summary>Gets the serialization.</summary>
    public ISerializationConfiguration Serialization { get; }
    /// <summary>Gets the transport.</summary>
    public ITransportConfiguration Transport { get; }

    /// <summary>Adds serializer to the configuration.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="isSerializer">The is serializer.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        Serialization.AddSerializer(factory, isSerializer);
    }

    /// <summary>Adds deserializer to the configuration.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="isDefault">The is default.</param>
    public void AddDeserializer(ISerializerFactory factory, bool isDefault = false)
    {
        Serialization.AddDeserializer(factory, isDefault);
    }

    /// <summary>Configures system text json serializer options.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
    {
        Serialization.ConfigureSystemTextJsonSerializerOptions(configure);
    }

    /// <summary>Clears serialization.</summary>
    public void ClearSerialization()
    {
        Serialization.Clear();
    }
}
