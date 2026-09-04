using System;
using System.Threading;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a message retry for a consumer, on the consumer configurator, which is constrained to
/// the message types for that consumer, and only applies to the consumer prior to the consumer factory.
/// </summary>
/// <typeparam name="TConsumer">The consumer type</typeparam>
public class MessageRetryConsumerConfigurationObserver<TConsumer> :
    IConsumerConfigurationObserver
    where TConsumer : class
{
    readonly CancellationToken _cancellationToken;
    readonly IConsumerConfigurator<TConsumer> _configurator;
    readonly Action<IRetryConfigurator> _configure;

    public MessageRetryConsumerConfigurationObserver(IConsumerConfigurator<TConsumer> configurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _cancellationToken = cancellationToken;
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    void IConsumerConfigurationObserver.ConsumerConfigured<T>(IConsumerConfigurator<T> configurator)
    {
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<T, TMessage>(IConsumerMessageConfigurator<T, TMessage> configurator)
    {
        if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(Batch<>), out Type[] types))
        {
            typeof(MessageRetryConsumerConfigurationObserver<TConsumer>)
                .GetMethod(nameof(BatchConsumerConfigured))
                .MakeGenericMethod(types[0])
                .Invoke(this, new object[] { configurator });
        }
        else
        {
            var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(Factory,
                _cancellationToken);

            _configure(specification);

            _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
        }
    }

    public void BatchConsumerConfigured<TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
        where TMessage : class
    {
        var consumerSpecification = configurator as IConsumerMessageSpecification<TConsumer, Batch<TMessage>>;
        if (consumerSpecification == null)
            throw new ArgumentException("The configurator must be a consumer specification");

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<Batch<TMessage>>, RetryConsumeContext<Batch<TMessage>>>(Factory,
            _cancellationToken);

        _configure(specification);

        consumerSpecification.AddPipeSpecification(specification);
    }

    static RetryConsumeContext<TMessage> Factory<TMessage>(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext retryContext)
        where TMessage : class
    {
        return new RetryConsumeContext<TMessage>(context, retryPolicy, retryContext);
    }
}
