using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the pipe for a consumer/message combination within a consumer configuration
/// block. Does not add any handlers to the message pipe standalone, everything is within
/// the consumer pipe segment.
/// </summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    public BatchConsumerMessageSpecification()
    {
        _batchConfigurator = new PipeConfigurator<ConsumerConsumeContext<TConsumer, Batch<TMessage>>>();
        _batchMessagePipeConfigurator = new PipeConfigurator<ConsumeContext<Batch<TMessage>>>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TMessage>();
        _observers = new ConsumerConfigurationObservable();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
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

    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Attempts to get message specification.</summary>
    /// <typeparam name="TC">The c type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">Receives the specification produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class
    {
        specification = this as IConsumerMessageSpecification<TC, T>;
        return specification != null
            || _consumerSpecification.TryGetMessageSpecification(out specification);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> specification)
    {
        _batchConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<Batch<TMessage>>> specification)
    {
        _batchMessagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Builds the configured component.</summary>
    /// <param name="consumeFilter">The consume filter.</param>
    /// <returns>The configured component.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> Build(IFilter<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> consumeFilter)
    {
        _batchConfigurator.UseFilter(consumeFilter);

        return _batchConfigurator.Build();
    }

    /// <summary>Builds message pipe.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured message pipe.</returns>
    public IPipe<ConsumeContext<Batch<TMessage>>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<Batch<TMessage>>>> configure)
    {
        configure?.Invoke(_batchMessagePipeConfigurator);

        return _batchMessagePipeConfigurator.Build();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _batchConfigurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, Batch<TMessage>>(specification));
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
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
