using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Defines consumer-specific endpoint naming, concurrency, and pipeline configuration used during automatic registration.
/// </summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerDefinition<TConsumer> :
    IConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    int? _concurrentMessageLimit;
    ConsumerConcurrencyPolicy? _concurrencyPolicy;
    string? _endpointName;

    /// <summary>Creates an empty consumer definition.</summary>
    protected ConsumerDefinition()
    {
    }

    /// <summary>
    /// Sets the transport-independent name of the receive endpoint that hosts the consumer.
    /// </summary>
    protected string EndpointName
    {
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _endpointName = value;
        }
    }

    /// <summary>Gets or sets the consumer's dedicated endpoint definition.</summary>
    public IEndpointDefinition<TConsumer>? EndpointDefinition { get; set; }

    IEndpointDefinition? IConsumerDefinition.EndpointDefinition => EndpointDefinition;

    /// <summary>Gets or sets the maximum number of messages this consumer may process concurrently.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        protected set
        {
            ConsumerConcurrencyPolicy? policy = value.HasValue
                ? ConsumerConcurrencyPolicy.Parallel(value.Value)
                : null;
            _concurrentMessageLimit = value;
            _concurrencyPolicy = policy;
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
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        if (_concurrencyPolicy is not null)
            consumerConfigurator.ConcurrencyPolicy = _concurrencyPolicy;
        ConfigureConsumer(endpointConfigurator, consumerConfigurator, context);
    }

    Type IConsumerDefinition.ConsumerType => typeof(TConsumer);

    string IConsumerDefinition.GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return string.IsNullOrWhiteSpace(_endpointName)
            ? _endpointName = EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Consumer<TConsumer>()
            : _endpointName!;
    }

    /// <summary>Creates a dedicated endpoint definition for the consumer.</summary>
    /// <param name="configure">The optional callback that configures the endpoint registration.</param>
    protected void Endpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<TConsumer>();

        configure?.Invoke(configurator);

        EndpointDefinition = new ConsumerEndpointDefinition<TConsumer>(configurator.Settings);
    }

    /// <summary>
    /// Configures the consumer pipeline after it is attached to a receive endpoint.
    /// </summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="consumerConfigurator">The consumer pipeline to configure.</param>
    /// <param name="context">The registration context that resolves registered dependencies.</param>
    protected virtual void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
    }
}
