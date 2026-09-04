using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the pipe for a consumer/message combination within a consumer configuration
/// block. Does not add any handlers to the message pipe standalone, everything is within
/// the consumer pipe segment.
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class BatchConsumerMessageSpecification<TConsumer, TMessage> :
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public BatchConsumerMessageSpecification()
    {
        _batchConfigurator = new PipeConfigurator<ConsumerConsumeContext<TConsumer, Batch<TMessage>>>();
        _batchMessagePipeConfigurator = new PipeConfigurator<ConsumeContext<Batch<TMessage>>>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TMessage>();
        _observers = new ConsumerConfigurationObservable();
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Message(Action<IConsumerMessageConfigurator<TMessage>> configure)
    {
        _consumerSpecification.Message(configure);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets the message type value.
    /// </summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>
    /// Attempts to get message specification.
    /// </summary>
    /// <typeparam name="TC">The tc type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class
    {
        specification = this as IConsumerMessageSpecification<TC, T>;
        return specification != null
            || _consumerSpecification.TryGetMessageSpecification(out specification);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> specification)
    {
        _batchConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<Batch<TMessage>>> specification)
    {
        _batchMessagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="consumeFilter">The consume filter value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> Build(IFilter<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> consumeFilter)
    {
        _batchConfigurator.UseFilter(consumeFilter);

        return _batchConfigurator.Build();
    }

    /// <summary>
    /// Performs the build message pipe operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<Batch<TMessage>>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<Batch<TMessage>>>> configure)
    {
        configure?.Invoke(_batchMessagePipeConfigurator);

        return _batchMessagePipeConfigurator.Build();
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _batchConfigurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, Batch<TMessage>>(specification));
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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
