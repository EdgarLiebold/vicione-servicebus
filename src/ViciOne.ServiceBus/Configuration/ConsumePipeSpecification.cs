using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consume pipe specification implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        lock (_lock)
        {
            _specifications.Add(specification);

            foreach (var messageSpecification in _messageSpecifications.Values)
                messageSpecification.AddPipeSpecification(specification);
        }
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _consumerObservers.Connect(observer);
    }

    /// <summary>
    /// Connects saga configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return _sagaObservers.Connect(observer);
    }

    /// <summary>
    /// Connects handler configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return _handlerObservers.Connect(observer);
    }

    /// <summary>
    /// Connects activity configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer)
    {
        return _activityObservers.Connect(observer);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
        where T : class
    {
        IMessageConsumePipeSpecification<T> messageSpecification = GetMessageSpecification<T>();

        messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds pre pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPrePipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        lock (_lock)
        {
            _prePipeSpecifications.Add(specification);

            foreach (var configurator in _consumePipeConfigurators)
                configurator.AddPrePipeSpecification(specification);
        }
    }

    /// <summary>
    /// Gets or sets the auto start value.
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// Consumes r configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        _consumerObservers.ConsumerConfigured(configurator);
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
        _consumerObservers.ConsumerMessageConfigured(configurator);
    }

    /// <summary>
    /// Performs the saga configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        _sagaObservers.SagaConfigured(configurator);
    }

    /// <summary>
    /// Performs the state machine saga configured operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
        where TInstance : class, ISaga, SagaStateMachineInstance
    {
        _sagaObservers.StateMachineSagaConfigured(configurator, stateMachine);
    }

    /// <summary>
    /// Performs the saga message configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _sagaObservers.SagaMessageConfigured(configurator);
    }

    /// <summary>
    /// Performs the handler configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
        where TMessage : class
    {
        _handlerObservers.HandlerConfigured(configurator);
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        _activityObservers.ActivityConfigured(configurator, compensateAddress);
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        _activityObservers.ExecuteActivityConfigured(configurator);
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        _activityObservers.CompensateActivityConfigured(configurator);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IMessageConsumePipeSpecification<T> GetMessageSpecification<T>()
        where T : class
    {
        lock (_lock)
        {
            var specification = _messageSpecifications.GetOrAdd(typeof(T), CreateMessageSpecification<T>);

            return specification.GetMessageSpecification<T>();
        }
    }

    /// <summary>
    /// Connects consume pipe specification observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipeSpecificationObserver(IConsumePipeSpecificationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Performs the build consume pipe operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IConsumePipe BuildConsumePipe()
    {
        var filter = new ConsumeContextMessageTypeFilter();

        IBuildPipeConfigurator<ConsumeContext> configurator = new PipeConfigurator<ConsumeContext>();

        for (var index = 0; index < _prePipeSpecifications.Count; index++)
            configurator.AddPipeSpecification(_prePipeSpecifications[index]);

        configurator.UseFilter(filter);

        return new ConsumePipe(this, filter, configurator.Build(), AutoStart);
    }

    /// <summary>
    /// Creates consume pipe specification.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
