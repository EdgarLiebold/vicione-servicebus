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
public class ConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    readonly IBuildPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>> _configurator;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _messagePipeConfigurator;
    readonly ConsumerConfigurationObservable _observers;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumerMessageSpecification()
    {
        _configurator = new PipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>>();
        _messagePipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _observers = new ConsumerConfigurationObservable();
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
            _observers.ForEach(observer => observer.ConsumerMessageConfigured(this)));

        return _configurator.Validate()
            .Concat(_messagePipeConfigurator.Validate())
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
        return specification != null;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        _messagePipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="consumeFilter">The consume filter value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, TMessage>> Build(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter)
    {
        _configurator.UseFilter(consumeFilter);

        return _configurator.Build();
    }

    /// <summary>
    /// Performs the build message pipe operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure)
    {
        configure?.Invoke(_messagePipeConfigurator);

        return _messagePipeConfigurator.Build();
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _configurator.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TMessage>(specification));
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
