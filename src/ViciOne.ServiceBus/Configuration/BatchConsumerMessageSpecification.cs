using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Builds the individual-message collection pipe and completed-batch consumer pipe for one
/// batch consumer contract.
/// </summary>
/// <typeparam name="TConsumer">The consumer that receives completed batches.</typeparam>
/// <typeparam name="TMessage">The message contract collected into batches.</typeparam>
internal sealed class BatchConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer, IMessageBatch<TMessage>>,
    IConsumerMessageConfigurator<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class, IConsumer<IMessageBatch<TMessage>>
{
    readonly IBuildPipeConfigurator<ConsumerConsumeContext<TConsumer, IMessageBatch<TMessage>>> _batchConfigurator;
    readonly IBuildPipeConfigurator<ConsumeContext<IMessageBatch<TMessage>>> _batchMessagePipeConfigurator;
    readonly ConsumerMessageSpecification<TConsumer, TMessage> _consumerSpecification;
    readonly ConsumerConfigurationObservable _observers;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Creates independent configurators for the individual-message and batch-message pipeline segments.</summary>
    public BatchConsumerMessageSpecification()
    {
        _batchConfigurator = new PipeConfigurator<ConsumerConsumeContext<TConsumer, IMessageBatch<TMessage>>>();
        _batchMessagePipeConfigurator = new PipeConfigurator<ConsumeContext<IMessageBatch<TMessage>>>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TMessage>();
        _observers = new ConsumerConfigurationObservable();
    }

    /// <summary>Adds middleware to the individual-message consumer segment.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Applies configuration to the individual-message consumer contract.</summary>
    /// <param name="configure">The message configuration callback.</param>
    public void Message(Action<IConsumerMessageConfigurator<TMessage>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _consumerSpecification.Message(configure);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
        {
            var batchSpecification = (IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>>)this;
            _observers.ForEach(observer => observer.ConsumerMessageConfigured(batchSpecification));
        });

        return _batchConfigurator.Validate()
            .Concat(_batchMessagePipeConfigurator.Validate())
            .Concat(_consumerSpecification.Validate())
            .ToArray();
    }

    /// <summary>Gets the individual message type collected by this specification.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Finds either this completed-batch specification or the nested individual-message specification.</summary>
    /// <typeparam name="TRequestedConsumer">The requested consumer type.</typeparam>
    /// <typeparam name="TRequestedMessage">The requested message type.</typeparam>
    /// <param name="specification">Receives the matching specification when one exists.</param>
    /// <returns><see langword="true" /> when a matching specification exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TRequestedConsumer, TRequestedMessage>(
        [NotNullWhen(true)] out IConsumerMessageSpecification<TRequestedConsumer, TRequestedMessage>? specification)
        where TRequestedMessage : class
        where TRequestedConsumer : class
    {
        specification = this as IConsumerMessageSpecification<TRequestedConsumer, TRequestedMessage>;
        return specification != null
            || _consumerSpecification.TryGetMessageSpecification(out specification);
    }

    /// <summary>Adds middleware to the completed-batch consumer segment.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, IMessageBatch<TMessage>>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _batchConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds middleware to the completed-batch message segment.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<IMessageBatch<TMessage>>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _batchMessagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Builds the completed-batch consumer segment with its terminal consume filter.</summary>
    /// <param name="consumeFilter">The terminal filter that invokes the application consumer.</param>
    /// <returns>The completed-batch consumer pipe.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, IMessageBatch<TMessage>>> Build(IFilter<ConsumerConsumeContext<TConsumer, IMessageBatch<TMessage>>> consumeFilter)
    {
        ArgumentNullException.ThrowIfNull(consumeFilter);

        _batchConfigurator.UseFilter(consumeFilter);

        return _batchConfigurator.Build();
    }

    /// <summary>Builds the completed-batch message segment after applying final connector configuration.</summary>
    /// <param name="configure">The final message-pipe configuration callback.</param>
    /// <returns>The completed-batch message pipe.</returns>
    public IPipe<ConsumeContext<IMessageBatch<TMessage>>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<IMessageBatch<TMessage>>>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_batchMessagePipeConfigurator);

        return _batchMessagePipeConfigurator.Build();
    }

    /// <summary>Projects consumer-wide middleware onto the completed-batch consumer segment.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _batchConfigurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, IMessageBatch<TMessage>>(specification));
    }

    /// <summary>Connects an observer to batch consumer configuration events.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Applies configuration to the completed-batch message segment.</summary>
    /// <param name="configure">The completed-batch configuration callback.</param>
    public void Message(Action<IConsumerMessageConfigurator<IMessageBatch<TMessage>>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(new ConsumerMessageConfigurator(_batchMessagePipeConfigurator));
    }


    sealed class ConsumerMessageConfigurator :
        IConsumerMessageConfigurator<IMessageBatch<TMessage>>
    {
        readonly IBuildPipeConfigurator<ConsumeContext<IMessageBatch<TMessage>>> _batchConfigurator;

        public ConsumerMessageConfigurator(IBuildPipeConfigurator<ConsumeContext<IMessageBatch<TMessage>>> batchConfigurator)
        {
            _batchConfigurator = batchConfigurator ?? throw new ArgumentNullException(nameof(batchConfigurator));
        }

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<IMessageBatch<TMessage>>> specification)
        {
            ArgumentNullException.ThrowIfNull(specification);

            _batchConfigurator.AddPipeSpecification(specification);
        }
    }
}
