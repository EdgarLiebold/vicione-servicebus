using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga specification implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaSpecification<TSaga> :
    OptionsSet,
    ISagaSpecification<TSaga>
    where TSaga : class, ISaga
{
    readonly ConnectHandle[] _handles;
    readonly IReadOnlyDictionary<Type, ISagaMessageSpecification<TSaga>> _messageTypes;
    /// <summary>
    /// Defines the observers value.
    /// </summary>
    protected readonly SagaConfigurationObservable Observers;
    IConcurrencyLimiter _concurrencyLimiter = null!;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageSpecifications">The message specifications value.</param>
    public SagaSpecification(IEnumerable<ISagaMessageSpecification<TSaga>> messageSpecifications)
    {
        _messageTypes = messageSpecifications.ToDictionary(x => x.MessageType);

        Observers = new SagaConfigurationObservable();
        _handles = _messageTypes.Values.Select(x => x.ConnectSagaConfigurationObserver(Observers)).ToArray();
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
        where T : class
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ISagaMessageSpecification<TSaga, T> specification = GetMessageSpecification<T>();

        configure(specification);
    }

    /// <summary>
    /// Performs the saga message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
        where T : class
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ISagaMessageSpecification<TSaga, T> specification = GetMessageSpecification<T>();

        configure(specification);
    }

    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class
    {
        if (!_messageTypes.TryGetValue(typeof(T), out ISagaMessageSpecification<TSaga>? specification))
            throw new ArgumentException($"MessageType {TypeCache<T>.ShortName} is not consumed by {TypeCache<TSaga>.ShortName}");

        return specification.GetMessageSpecification<T>();
    }

    /// <summary>
    /// Configures message pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator value.</param>
    public void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class
    {
        if (ConcurrentMessageLimit.HasValue)
        {
            _concurrencyLimiter ??= new ConcurrencyLimiter(ConcurrentMessageLimit.Value, TypeCache<TSaga>.ShortName);

            pipeConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<T>(_concurrencyLimiter));
        }
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        NotifyConfigurationObservers();

        return _messageTypes.Values.SelectMany(x => x.Validate())
            .Concat(ValidateOptions())
            .ToArray();
    }

    /// <summary>
    /// Performs the notify configuration observers operation.
    /// </summary>
    protected void NotifyConfigurationObservers()
    {
        _configurationNotification.EnsureNotified(() =>
            Observers.ForEach(observer => observer.SagaConfigured(this)));
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
    {
        foreach (ISagaMessageSpecification<TSaga> messageSpecification in _messageTypes.Values)
            messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Connects saga configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return Observers.Connect(observer);
    }
}
