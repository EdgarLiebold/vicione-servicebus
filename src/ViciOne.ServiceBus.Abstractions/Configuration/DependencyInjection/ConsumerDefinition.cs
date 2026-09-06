using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// A consumer definition defines the configuration for a consumer, which can be used by the automatic registration code to
/// configure the consumer on a receive endpoint.
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
public class ConsumerDefinition<TConsumer> :
    IConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    int? _concurrentMessageLimit;
    ConsumerConcurrencyPolicy? _concurrencyPolicy;
    string? _endpointName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected ConsumerDefinition()
    {
    }

    /// <summary>
    /// Specify the endpoint name (which may be a queue, or a subscription, depending upon the transport) on which the consumer
    /// should be configured.
    /// </summary>
    protected string EndpointName
    {
        set => _endpointName = value;
    }

    /// <summary>
    /// Gets or sets the endpoint definition value.
    /// </summary>
    public IEndpointDefinition<TConsumer>? EndpointDefinition { get; set; }

    IEndpointDefinition? IConsumerDefinition.EndpointDefinition => EndpointDefinition;

    /// Set the concurrent message limit for the consumer, which limits how many consumers are able to concurrently
    /// consume messages.
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        protected set
        {
            _concurrentMessageLimit = value;
            _concurrencyPolicy = value.HasValue
                ? ConsumerConcurrencyPolicy.Parallel(value.Value)
                : null;
        }
    }

    /// <summary>
    /// Sets the consumer-local serial or fixed-parallel execution policy. Partitioned policies
    /// require a typed selector and are configured in <see cref="ConfigureConsumer"/>.
    /// </summary>
    protected ConsumerConcurrencyPolicy? ConcurrencyPolicy
    {
        get => _concurrencyPolicy;
        set
        {
            if (value?.Mode == ConsumerConcurrencyMode.Partitioned)
            {
                throw new ArgumentException(
                    "Partitioned concurrency requires a strongly typed message and partition-key selector.",
                    nameof(value));
            }

            _concurrencyPolicy = value;
            _concurrentMessageLimit = value?.Mode == ConsumerConcurrencyMode.Parallel
                ? value.Concurrency
                : null;
        }
    }

    void IConsumerDefinition<TConsumer>.Configure(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        if (_concurrencyPolicy is not null)
            consumerConfigurator.ConcurrencyPolicy = _concurrencyPolicy;
        ConfigureConsumer(endpointConfigurator, consumerConfigurator, context);
    }

    Type IConsumerDefinition.ConsumerType => typeof(TConsumer);

    string IConsumerDefinition.GetEndpointName(IEndpointNameFormatter formatter)
    {
        return string.IsNullOrWhiteSpace(_endpointName)
            ? _endpointName = EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Consumer<TConsumer>()
            : _endpointName!;
    }

    /// <summary>
    /// Configure the consumer endpoint
    /// </summary>
    /// <param name="configure"></param>
    protected void Endpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<TConsumer>();

        configure?.Invoke(configurator);

        EndpointDefinition = new ConsumerEndpointDefinition<TConsumer>(configurator.Settings);
    }

    /// <summary>
    /// Called when the consumer is being configured on the endpoint. Configuration only applies to this consumer, and does not apply to
    /// the endpoint.
    /// </summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer</param>
    /// <param name="consumerConfigurator">The consumer configurator</param>
    /// <param name="context"></param>
    protected virtual void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
    }
}
