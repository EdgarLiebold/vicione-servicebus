#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Middleware;


    public class ConsumerSpecification<TConsumer> :
        OptionsSet,
        IConsumerSpecification<TConsumer>
        where TConsumer : class
    {
        readonly ConnectHandle[] _handles;
        readonly IReadOnlyDictionary<Type, IConsumerMessageSpecification<TConsumer>> _messageTypes;
        readonly ConsumerConfigurationObservable _observers;
        readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();
        IConcurrencyLimiter? _concurrencyLimiter;

        public ConsumerSpecification(IEnumerable<IConsumerMessageSpecification<TConsumer>> messageSpecifications)
        {
            _messageTypes = messageSpecifications.ToDictionary(x => x.MessageType);

            _observers = new ConsumerConfigurationObservable();
            _handles = _messageTypes.Values.Select(x => x.ConnectConsumerConfigurationObserver(_observers)).ToArray();
        }

        public int? ConcurrentMessageLimit { get; set; }

        public void Message<T>(Action<IConsumerMessageConfigurator<T>>? configure)
            where T : class
        {
            IConsumerMessageSpecification<TConsumer, T> specification = GetMessageSpecification<T>();

            configure?.Invoke(specification);
        }

        public void ConsumerMessage<T>(Action<IConsumerMessageConfigurator<TConsumer, T>>? configure)
            where T : class
        {
            IConsumerMessageSpecification<TConsumer, T> specification = GetMessageSpecification<T>();

            configure?.Invoke(specification);
        }

        public IConsumerMessageSpecification<TConsumer, T> GetMessageSpecification<T>()
            where T : class
        {
            foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
            {
                if (messageSpecification.TryGetMessageSpecification(out IConsumerMessageSpecification<TConsumer, T> result))
                    return result;
            }

            throw new ArgumentException($"MessageType {TypeCache<T>.ShortName} is not consumed by {TypeCache<TConsumer>.ShortName}");
        }

        public void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
            where T : class
        {
            if (ConcurrentMessageLimit.HasValue)
            {
                _concurrencyLimiter ??= new ConcurrencyLimiter(ConcurrentMessageLimit.Value, TypeCache<TConsumer>.ShortName);

                pipeConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<T>(_concurrencyLimiter));
            }
        }

        public IEnumerable<ValidationResult> Validate()
        {
            _configurationNotification.EnsureNotified(() =>
                _observers.ForEach(observer => observer.ConsumerConfigured(this)));

            return _messageTypes.Values.SelectMany(x => x.Validate())
                .Concat(ValidateOptions())
                .ToArray();
        }

        public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
        {
            foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
                messageSpecification.AddPipeSpecification(specification);
        }

        public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
        {
            return _observers.Connect(observer);
        }
    }
}
