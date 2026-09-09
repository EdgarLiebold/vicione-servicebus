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
    IConsumerMessageSpecification<TConsumer, Batch<TMessage>>,
    IConsumerMessageConfigurator<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class, IConsumer<Batch<TMessage>>
{
    readonly IBuildPipeConfigurator<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> _batchConfigurator;
    readonly IBuildPipeConfigurator<ConsumeContext<Batch<TMessage>>> _batchMessagePipeConfigurator;
    readonly ConsumerMessageSpecification<TConsumer, TMessage> _consumerSpecification;
    readonly ConsumerConfigurationObservable _observers;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Creates independent configurators for the individual-message and batch-message pipeline segments.</summary>
    public BatchConsumerMessageSpecification()
    {
        _batchConfigurator = new PipeConfigurator<ConsumerConsumeContext<TConsumer, Batch<TMessage>>>();
        _batchMessagePipeConfigurator = new PipeConfigurator<ConsumeContext<Batch<TMessage>>>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TMessage>();
        _observers = new ConsumerConfigurationObservable();
    }

    /// <summary>Adds middleware to the individual-message consumer segment.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Applies configuration to the individual-message consumer contract.</summary>
    /// <param name="configure">The message configuration callback.</param>
    public void Message(Action<IConsumerMessageConfigurator<TMessage>> configure)
    {
        _consumerSpecification.Message(configure);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
        {
            var batchSpecification = (IConsumerMessageConfigurator<TConsumer, Batch<TMessage>>)this;
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
    /// <typeparam name="TC">The requested consumer type.</typeparam>
    /// <typeparam name="T">The requested message type.</typeparam>
    /// <param name="specification">Receives the matching specification when one exists.</param>
    /// <returns><see langword="true" /> when a matching specification exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class
    {
        specification = this as IConsumerMessageSpecification<TC, T>;
        return specification != null
            || _consumerSpecification.TryGetMessageSpecification(out specification);
    }

    /// <summary>Adds middleware to the completed-batch consumer segment.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> specification)
    {
        _batchConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds middleware to the completed-batch message segment.</summary>
    /// <param name="specification">The middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<Batch<TMessage>>> specification)
    {
        _batchMessagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Builds the completed-batch consumer segment with its terminal consume filter.</summary>
    /// <param name="consumeFilter">The terminal filter that invokes the application consumer.</param>
    /// <returns>The completed-batch consumer pipe.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> Build(IFilter<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> consumeFilter)
    {
        _batchConfigurator.UseFilter(consumeFilter);

        return _batchConfigurator.Build();
    }

    /// <summary>Builds the completed-batch message segment after applying final connector configuration.</summary>
    /// <param name="configure">The final message-pipe configuration callback.</param>
    /// <returns>The completed-batch message pipe.</returns>
    public IPipe<ConsumeContext<Batch<TMessage>>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<Batch<TMessage>>>> configure)
    {
        configure?.Invoke(_batchMessagePipeConfigurator);

        return _batchMessagePipeConfigurator.Build();
    }

    /// <summary>Projects consumer-wide middleware onto the completed-batch consumer segment.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _batchConfigurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, Batch<TMessage>>(specification));
    }

    /// <summary>Connects an observer to batch consumer configuration events.</summary>
    /// <param name="observer">The observer to register.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Applies configuration to the completed-batch message segment.</summary>
    /// <param name="configure">The completed-batch configuration callback.</param>
    public void Message(Action<IConsumerMessageConfigurator<Batch<TMessage>>> configure)
    {
        configure?.Invoke(new ConsumerMessageConfigurator(_batchMessagePipeConfigurator));
    }


    class ConsumerMessageConfigurator :
        IConsumerMessageConfigurator<Batch<TMessage>>
    {
        readonly IBuildPipeConfigurator<ConsumeContext<Batch<TMessage>>> _batchConfigurator;

        public ConsumerMessageConfigurator(IBuildPipeConfigurator<ConsumeContext<Batch<TMessage>>> batchConfigurator)
        {
            _batchConfigurator = batchConfigurator;
        }

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<Batch<TMessage>>> specification)
        {
            _batchConfigurator.AddPipeSpecification(specification);
        }
    }
}
