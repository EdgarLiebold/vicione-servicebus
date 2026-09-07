using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaSpecification<TSaga> :
    OptionsSet,
    ISagaSpecification<TSaga>
    where TSaga : class, ISaga
{
    readonly ConnectHandle[] _handles;
    readonly IReadOnlyDictionary<Type, ISagaMessageSpecification<TSaga>> _messageTypes;
    /// <summary>Exposes the observers used by the containing type.</summary>
    protected readonly SagaConfigurationObservable Observers;
    IConcurrencyLimiter _concurrencyLimiter = null!;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageSpecifications">The message specifications.</param>
    public SagaSpecification(IEnumerable<ISagaMessageSpecification<TSaga>> messageSpecifications)
    {
        _messageTypes = messageSpecifications.ToDictionary(x => x.MessageType);

        Observers = new SagaConfigurationObservable();
        _handles = _messageTypes.Values.Select(x => x.ConnectSagaConfigurationObserver(Observers)).ToArray();
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
        where T : class
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ISagaMessageSpecification<TSaga, T> specification = GetMessageSpecification<T>();

        configure(specification);
    }

    /// <summary>Configures the saga message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
        where T : class
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ISagaMessageSpecification<TSaga, T> specification = GetMessageSpecification<T>();

        configure(specification);
    }

    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    public ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class
    {
        if (!_messageTypes.TryGetValue(typeof(T), out ISagaMessageSpecification<TSaga>? specification))
            throw new ArgumentException($"MessageType {TypeCache<T>.ShortName} is not consumed by {TypeCache<TSaga>.ShortName}");

        return specification.GetMessageSpecification<T>();
    }

    /// <summary>Configures message pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator.</param>
    public void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class
    {
        if (ConcurrentMessageLimit.HasValue)
        {
            _concurrencyLimiter ??= new ConcurrencyLimiter(ConcurrentMessageLimit.Value, TypeCache<TSaga>.ShortName);

            pipeConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<T>(_concurrencyLimiter));
        }
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        NotifyConfigurationObservers();

        return _messageTypes.Values.SelectMany(x => x.Validate())
            .Concat(ValidateOptions())
            .ToArray();
    }

    /// <summary>Notifies registered observers about configuration observers.</summary>
    protected void NotifyConfigurationObservers()
    {
        _configurationNotification.EnsureNotified(() =>
            Observers.ForEach(observer => observer.SagaConfigured(this)));
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
    {
        foreach (ISagaMessageSpecification<TSaga> messageSpecification in _messageTypes.Values)
            messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Connects saga configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return Observers.Connect(observer);
    }
}
