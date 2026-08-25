namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using Consumer;
    using Internals;


    public class UntypedConsumerConfigurator<TConsumer> :
        IConsumerConfigurator,
        IReceiveEndpointSpecification
        where TConsumer : class
    {
        readonly IConsumerFactory<TConsumer> _consumerFactory;
        readonly IConsumerSpecification<TConsumer> _specification;

        public UntypedConsumerConfigurator(Func<Type, object> consumerFactory, IConsumerConfigurationObserver observer)
        {
            ArgumentNullException.ThrowIfNull(consumerFactory);
            ArgumentNullException.ThrowIfNull(observer);

            _consumerFactory = new DelegateConsumerFactory<TConsumer>(() => (TConsumer)consumerFactory(typeof(TConsumer)));

            _specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();

            _specification.ConnectConsumerConfigurationObserver(observer);
        }

        public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
        {
            return _specification.ConnectConsumerConfigurationObserver(observer);
        }

        public int? ConcurrentMessageLimit
        {
            set => _specification.ConcurrentMessageLimit = value;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (!typeof(TConsumer).ImplementsInterface<IConsumer>())
                yield return this.Warning($"The consumer class {TypeCache<TConsumer>.ShortName} does not implement any IMessageConsumer interfaces");

            foreach (var result in _specification.Validate())
                yield return result;
        }

        public void Configure(IReceiveEndpointBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            ConsumerConnectorCache<TConsumer>.Connector.ConnectConsumer(builder, _consumerFactory, _specification);
        }
    }
}
