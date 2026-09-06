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
public class ConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    readonly IBuildPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> _configurator;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _messagePipeConfigurator;
    readonly ConsumerConfigurationObservable _observers;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Initializes a new instance.</summary>
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
        return specification != null;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        _messagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Builds the configured component.</summary>
    /// <param name="consumeFilter">The consume filter.</param>
    /// <returns>The configured component.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, TMessage>> Build(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter)
    {
        _configurator.UseFilter(consumeFilter);

        return _configurator.Build();
    }

    /// <summary>Builds message pipe.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured message pipe.</returns>
    public IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure)
    {
        configure?.Invoke(_messagePipeConfigurator);

        return _messagePipeConfigurator.Build();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _configurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TMessage>(specification));
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
    public void Message(Action<IConsumerMessageConfigurator<TMessage>> configure)
    {
        configure?.Invoke(new ConsumerMessageConfigurator(_configurator));
    }


    class ConsumerMessageConfigurator :
        IConsumerMessageConfigurator<TMessage>
    {
        readonly IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> _configurator;

        public ConsumerMessageConfigurator(IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> configurator)
        {
            _configurator = configurator;
        }

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            _configurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TMessage>(specification));
        }
    }
}
