using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consume pipe.</summary>
public class ConsumePipeSpecification :
    IConsumePipeConfigurator,
    IConsumePipeSpecification
{
    readonly ActivityConfigurationObservable _activityObservers;
    readonly List<IConsumePipeConfigurator> _consumePipeConfigurators;
    readonly ConsumerConfigurationObservable _consumerObservers;
    readonly HandlerConfigurationObservable _handlerObservers;
    readonly object _lock = new object();
    readonly IDictionary<Type, IMessageConsumePipeSpecification> _messageSpecifications;
    readonly ConsumePipeSpecificationObservable _observers;
    readonly List<IPipeSpecification<ConsumeContext>> _prePipeSpecifications;
    readonly SagaConfigurationObservable _sagaObservers;
    readonly List<IPipeSpecification<ConsumeContext>> _specifications;

    /// <summary>Initializes a new instance.</summary>
    public ConsumePipeSpecification()
    {
        _prePipeSpecifications = new List<IPipeSpecification<ConsumeContext>>();
        _specifications = new List<IPipeSpecification<ConsumeContext>>();
        _messageSpecifications = new Dictionary<Type, IMessageConsumePipeSpecification>();
        _consumePipeConfigurators = new List<IConsumePipeConfigurator>();
        _observers = new ConsumePipeSpecificationObservable();

        _consumerObservers = new ConsumerConfigurationObservable();
        _sagaObservers = new SagaConfigurationObservable();
        _handlerObservers = new HandlerConfigurationObservable();
        _activityObservers = new ActivityConfigurationObservable();

        AutoStart = true;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        lock (_lock)
        {
            _specifications.Add(specification);

            foreach (var messageSpecification in _messageSpecifications.Values)
                messageSpecification.AddPipeSpecification(specification);
        }
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _consumerObservers.Connect(observer);
    }

    /// <summary>Connects saga configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return _sagaObservers.Connect(observer);
    }

    /// <summary>Connects handler configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return _handlerObservers.Connect(observer);
    }

    /// <summary>Connects activity configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer)
    {
        return _activityObservers.Connect(observer);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
        where T : class
    {
        IMessageConsumePipeSpecification<T> messageSpecification = GetMessageSpecification<T>();

        messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Adds pre pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        lock (_lock)
        {
            _prePipeSpecifications.Add(specification);

            foreach (var configurator in _consumePipeConfigurators)
                configurator.AddPrePipeSpecification(specification);
        }
    }

    /// <summary>Gets or sets the auto start.</summary>
    public bool AutoStart { get; set; }

    /// <summary>Consumes r configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        _consumerObservers.ConsumerConfigured(configurator);
    }

    /// <summary>Consumes r message configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        _consumerObservers.ConsumerMessageConfigured(configurator);
    }

    /// <summary>Reports that saga has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        _sagaObservers.SagaConfigured(configurator);
    }

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        _sagaObservers.StateMachineSagaConfigured(configurator, stateMachine);
    }

    /// <summary>Reports that saga message has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        _sagaObservers.SagaMessageConfigured(configurator);
    }

    /// <summary>Reports that handler has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        _handlerObservers.HandlerConfigured(configurator);
    }

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        _activityObservers.ActivityConfigured(configurator, compensateAddress);
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        _activityObservers.ExecuteActivityConfigured(configurator);
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        _activityObservers.CompensateActivityConfigured(configurator);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in _specifications.SelectMany(x => x.Validate()))
            yield return result;

        foreach (var result in _prePipeSpecifications.SelectMany(x => x.Validate()))
            yield return result;

        lock (_lock)
        {
            foreach (var result in _messageSpecifications.Values.SelectMany(x => x.Validate()))
                yield return result;
        }
    }

    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    public IMessageConsumePipeSpecification<T> GetMessageSpecification<T>()
        where T : class
    {
        lock (_lock)
        {
            var specification = _messageSpecifications.GetOrAdd(typeof(T), CreateMessageSpecification<T>);

            return specification.GetMessageSpecification<T>();
        }
    }

    /// <summary>Connects consume pipe specification observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipeSpecificationObserver(IConsumePipeSpecificationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Builds consume pipe.</summary>
    /// <returns>The configured consume pipe.</returns>
    public IConsumePipe BuildConsumePipe()
    {
        var filter = new ConsumeContextMessageTypeFilter();

        IBuildPipeConfigurator<ConsumeContext> configurator = new PipeConfigurator<ConsumeContext>();

        for (var index = 0; index < _prePipeSpecifications.Count; index++)
            configurator.AddPipeSpecification(_prePipeSpecifications[index]);

        configurator.UseFilter(filter);

        return new ConsumePipe(this, filter, configurator.Build(), AutoStart);
    }

    /// <summary>Creates consume pipe specification.</summary>
    /// <returns>The created consume pipe specification.</returns>
    public IConsumePipeSpecification CreateConsumePipeSpecification()
    {
        var specification = new ConsumePipeSpecification();

        for (var index = 0; index < _prePipeSpecifications.Count; index++)
            specification.AddPrePipeSpecification(_prePipeSpecifications[index]);

        lock (_lock)
            _consumePipeConfigurators.Add(specification);

        return specification;
    }

    IMessageConsumePipeSpecification CreateMessageSpecification<T>(Type type)
        where T : class
    {
        var specification = new MessageConsumePipeSpecification<T>();

        for (var index = 0; index < _specifications.Count; index++)
            specification.AddPipeSpecification(_specifications[index]);

        _observers.MessageSpecificationCreated(specification);

        return specification;
    }
}
