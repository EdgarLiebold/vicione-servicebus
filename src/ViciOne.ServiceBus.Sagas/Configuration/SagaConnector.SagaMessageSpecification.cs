using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Configures the pipe for a Saga/message combination within a Saga configuration
    /// block. Does not add any handlers to the message pipe standalone, everything is within
    /// the Saga pipe segment.
    /// </summary>
    public class SagaMessageSpecification :
        ISagaMessageSpecification<TSaga, TMessage>
    {
        readonly IBuildPipeConfigurator<SagaConsumeContext<TSaga, TMessage>> _configurator;
        readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _messagePipeConfigurator;
        readonly SagaConfigurationObservable _observers;
        readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

        /// <summary>Initializes a new instance.</summary>
        public SagaMessageSpecification()
        {
            _configurator = new PipeConfigurator<SagaConsumeContext<TSaga, TMessage>>();
            _messagePipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
            _observers = new SagaConfigurationObservable();
        }

        /// <summary>Validates the current configuration.</summary>
        /// <returns>The validation failures.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            _configurationNotification.EnsureNotified(() =>
                _observers.ForEach(observer => observer.SagaMessageConfigured(this)));

            return _configurator.Validate()
                .Concat(_messagePipeConfigurator.Validate())
                .ToArray();
        }

        /// <summary>Gets the message type.</summary>
        public Type MessageType => typeof(TMessage);

        ISagaMessageSpecification<TSaga, T> ISagaMessageSpecification<TSaga>.GetMessageSpecification<T>()
        {
            if (this is ISagaMessageSpecification<TSaga, T> result)
                return result;

            throw new ArgumentException($"The message type was invalid: {TypeCache<T>.ShortName}");
        }

        /// <summary>Adds pipe specification to the configuration.</summary>
        /// <param name="specification">The specification.</param>
        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga, TMessage>> specification)
        {
            _configurator.AddPipeSpecification(specification);
        }

        /// <summary>Adds pipe specification to the configuration.</summary>
        /// <param name="specification">The specification.</param>
        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            _messagePipeConfigurator.AddPipeSpecification(specification);
        }

        /// <summary>Builds consumer pipe.</summary>
        /// <param name="consumeFilter">The consume filter.</param>
        /// <returns>The configured consumer pipe.</returns>
        public IPipe<SagaConsumeContext<TSaga, TMessage>> BuildConsumerPipe(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter)
        {
            ArgumentNullException.ThrowIfNull(consumeFilter);

            _configurator.AddPipeSpecification(new FilterPipeSpecification<SagaConsumeContext<TSaga, TMessage>>(consumeFilter));

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
        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            _configurator.AddPipeSpecification(new SagaPipeSpecificationProxy(specification));
        }

        /// <summary>Connects saga configuration observer.</summary>
        /// <param name="observer">The observer to connect.</param>
        /// <returns>A handle that disconnects the registration.</returns>
        public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
        {
            return _observers.Connect(observer);
        }

        /// <summary>Applies the message configuration.</summary>
        /// <param name="configure">The callback used to configure the component.</param>
        public void Message(Action<ISagaMessageConfigurator<TMessage>> configure)
        {
            configure?.Invoke(new SagaMessageConfigurator(_configurator));
        }


        class SagaMessageConfigurator :
            ISagaMessageConfigurator<TMessage>
        {
            readonly IPipeConfigurator<SagaConsumeContext<TSaga, TMessage>> _configurator;

            public SagaMessageConfigurator(IPipeConfigurator<SagaConsumeContext<TSaga, TMessage>> configurator)
            {
                _configurator = configurator;
            }

            public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
            {
                _configurator.AddPipeSpecification(new SagaPipeSpecificationProxy(specification));
            }
        }
    }
}
