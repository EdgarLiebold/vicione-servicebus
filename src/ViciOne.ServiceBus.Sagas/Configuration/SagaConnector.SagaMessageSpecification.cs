using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Configures separate message-context and saga/message-context pipelines and notifies saga configuration observers.
    /// </summary>
    public class SagaMessageSpecification :
        ISagaMessageSpecification<TSaga, TMessage>
    {
        readonly IBuildPipeConfigurator<SagaConsumeContext<TSaga, TMessage>> _configurator;
        readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _messagePipeConfigurator;
        readonly SagaConfigurationObservable _observers;
        readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

        /// <summary>Creates empty message and saga pipelines with a configuration-observer collection.</summary>
        public SagaMessageSpecification()
        {
            _configurator = new PipeConfigurator<SagaConsumeContext<TSaga, TMessage>>();
            _messagePipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
            _observers = new SagaConfigurationObservable();
        }

        /// <summary>Ensures configuration observers are notified, then materializes validation from both pipelines.</summary>
        /// <returns>Saga-pipeline results followed by message-pipeline results.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            _configurationNotification.EnsureNotified(() =>
                _observers.ForEach(observer => observer.SagaMessageConfigured(this)));

            return _configurator.Validate()
                .Concat(_messagePipeConfigurator.Validate())
                .ToArray();
        }

        /// <summary>Gets the message contract configured by this specification.</summary>
        public Type MessageType => typeof(TMessage);

        ISagaMessageSpecification<TSaga, T> ISagaMessageSpecification<TSaga>.GetMessageSpecification<T>()
        {
            if (this is ISagaMessageSpecification<TSaga, T> result)
                return result;

            throw new ArgumentException($"The message type was invalid: {TypeCache<T>.ShortName}");
        }

        /// <summary>Appends a saga/message-context specification to the saga-specific pipeline.</summary>
        /// <param name="specification">The specification applied after the saga instance is selected.</param>
        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga, TMessage>> specification)
        {
            _configurator.AddPipeSpecification(specification);
        }

        /// <summary>Appends a message-context specification to the message pipeline.</summary>
        /// <param name="specification">The specification applied before saga repository dispatch.</param>
        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            _messagePipeConfigurator.AddPipeSpecification(specification);
        }

        /// <summary>Appends the required consume filter and builds the saga-specific pipeline.</summary>
        /// <param name="consumeFilter">The filter appended after previously registered saga specifications.</param>
        /// <returns>The pipeline handling the selected saga/message context.</returns>
        public IPipe<SagaConsumeContext<TSaga, TMessage>> BuildConsumerPipe(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter)
        {
            ArgumentNullException.ThrowIfNull(consumeFilter);

            _configurator.AddPipeSpecification(new FilterPipeSpecification<SagaConsumeContext<TSaga, TMessage>>(consumeFilter));

            return _configurator.Build();
        }

        /// <summary>Applies a supplied callback to the message configuration and builds the message pipeline.</summary>
        /// <param name="configure">The callback appending message filters; a null callback adds no configuration.</param>
        /// <returns>The pipeline composed from the accumulated message specifications.</returns>
        public IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure)
        {
            configure?.Invoke(_messagePipeConfigurator);

            return _messagePipeConfigurator.Build();
        }

        /// <summary>Adapts a saga-context specification into the saga/message-context pipeline.</summary>
        /// <param name="specification">The specification whose filters receive the saga-only context view.</param>
        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            _configurator.AddPipeSpecification(new SagaPipeSpecificationProxy(specification));
        }

        /// <summary>Registers an observer for saga-message configuration notification.</summary>
        /// <param name="observer">The observer receiving this message specification when configuration is validated.</param>
        /// <returns>A handle that disconnects the registration.</returns>
        public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
        {
            return _observers.Connect(observer);
        }

        /// <summary>Applies a supplied callback through a message-context adapter into the saga-specific pipeline.</summary>
        /// <param name="configure">The callback adding adapted filters; a null callback adds no configuration.</param>
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
