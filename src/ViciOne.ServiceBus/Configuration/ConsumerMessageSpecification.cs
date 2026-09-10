using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the pipe for a consumer/message combination within a consumer configuration
/// block. Does not add any handlers to the message pipe standalone, everything is within
/// the consumer pipe segment.
/// </summary>
/// <typeparam name="TConsumer">The consumer implementation configured by this specification.</typeparam>
/// <typeparam name="TMessage">The message contract delivered to that consumer.</typeparam>
public sealed class ConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    readonly IBuildPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> _configurator;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _messagePipeConfigurator;
    readonly ConsumerConfigurationObservable _observers;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Creates an empty consumer-message pipeline with its own configuration observer stream.</summary>
    public ConsumerMessageSpecification()
    {
        _configurator = new PipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>>();
        _messagePipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _observers = new ConsumerConfigurationObservable();
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
            _observers.ForEach(observer => observer.ConsumerMessageConfigured(this)));

        return _configurator.Validate()
            .Concat(_messagePipeConfigurator.Validate())
            .ToArray();
    }

    /// <summary>Gets the message contract configured by this specification.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Attempts to project this specification to a requested consumer and message pair.</summary>
    /// <typeparam name="TRequestedConsumer">The requested consumer type.</typeparam>
    /// <typeparam name="TRequestedMessage">The requested message contract.</typeparam>
    /// <param name="specification">Receives this specification when both requested types match.</param>
    /// <returns><see langword="true" /> when this instance implements the requested closed specification type.</returns>
    public bool TryGetMessageSpecification<TRequestedConsumer, TRequestedMessage>(
        [NotNullWhen(true)] out IConsumerMessageSpecification<TRequestedConsumer, TRequestedMessage>? specification)
        where TRequestedMessage : class
        where TRequestedConsumer : class
    {
        specification = this as IConsumerMessageSpecification<TRequestedConsumer, TRequestedMessage>;
        return specification != null;
    }

    /// <summary>Adds middleware scoped to this closed consumer-message context.</summary>
    /// <param name="specification">The consumer-message middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds middleware scoped to this message contract.</summary>
    /// <param name="specification">The message-level middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _messagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Appends the consumer invocation filter and builds the closed consumer-message pipe.</summary>
    /// <param name="consumeFilter">The terminal filter that invokes the consumer contract.</param>
    /// <returns>The configured consumer-message pipe.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, TMessage>> Build(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter)
    {
        ArgumentNullException.ThrowIfNull(consumeFilter);

        _configurator.UseFilter(consumeFilter);

        return _configurator.Build();
    }

    /// <summary>Applies final message-level configuration and builds the inbound message pipe.</summary>
    /// <param name="configure">The callback that appends the consumer dispatch stage.</param>
    /// <returns>The configured inbound message pipe.</returns>
    public IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_messagePipeConfigurator);

        return _messagePipeConfigurator.Build();
    }

    /// <summary>Projects consumer-wide middleware onto this message contract.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _configurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TMessage>(specification));
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <param name="configure">The callback that adds middleware to this consumer-message pipe.</param>
    public void Message(Action<IConsumerMessageConfigurator<TMessage>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(new ConsumerMessageConfigurator(_configurator));
    }


    sealed class ConsumerMessageConfigurator :
        IConsumerMessageConfigurator<TMessage>
    {
        readonly IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> _configurator;

        public ConsumerMessageConfigurator(IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> configurator)
        {
            _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        }

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            ArgumentNullException.ThrowIfNull(specification);

            _configurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TMessage>(specification));
        }
    }
}
