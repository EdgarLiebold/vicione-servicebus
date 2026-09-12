using System;
using System.Threading;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds retry specifications to the message pipelines of one consumer type.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public class MessageRetryConsumerConfigurationObserver<TConsumer> :
    IConsumerConfigurationObserver
    where TConsumer : class
{
    readonly CancellationToken _cancellationToken;
    readonly IConsumerConfigurator<TConsumer> _configurator;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>Creates an observer for a consumer and a shared retry policy callback.</summary>
    /// <param name="configurator">The consumer whose message pipelines receive retry handling.</param>
    /// <param name="cancellationToken">The token observed while retry delays are pending.</param>
    /// <param name="configure">The callback applied to each retry policy.</param>
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
        if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(IMessageBatch<>), out Type[] types))
        {
            var method = typeof(MessageRetryConsumerConfigurationObserver<TConsumer>)
                .GetMethod(nameof(BatchConsumerConfigured))
                ?? throw new InvalidOperationException($"The {nameof(BatchConsumerConfigured)} method was not found.");

            method
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

    /// <summary>Adds retry handling around a configured batch-consumer invocation.</summary>
    /// <typeparam name="TMessage">The message contract contained by the batch.</typeparam>
    /// <param name="configurator">The configured batch-consumer pipeline.</param>
    public void BatchConsumerConfigured<TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
        where TMessage : class
    {
        var consumerSpecification = configurator as IConsumerMessageSpecification<TConsumer, IMessageBatch<TMessage>>;
        if (consumerSpecification == null)
            throw new ArgumentException("The configurator must be a consumer specification");

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<IMessageBatch<TMessage>>, RetryConsumeContext<IMessageBatch<TMessage>>>(Factory,
            _cancellationToken);

        _configure(specification);

        consumerSpecification.AddPipeSpecification(specification);
    }

    static RetryConsumeContext<TMessage> Factory<TMessage>(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        where TMessage : class
    {
        return new RetryConsumeContext<TMessage>(context, retryPolicy, retryContext);
    }
}
