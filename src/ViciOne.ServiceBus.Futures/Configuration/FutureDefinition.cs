using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines reusable endpoint and concurrency configuration for a future state machine.
/// </summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
public abstract class FutureDefinition<TFuture> :
    IFutureDefinition<TFuture>
    where TFuture : class, SagaStateMachine<FutureState>
{
    int? _concurrentMessageLimit;
    string? _endpointName;

    /// <summary>Initializes a future definition with convention-based endpoint settings.</summary>
    protected FutureDefinition()
    {
    }

    /// <summary>Sets the transport entity name of the endpoint that hosts the future.</summary>
    protected string EndpointName
    {
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _endpointName = value;
        }
    }

    /// <summary>Gets or sets the endpoint definition used to host the future.</summary>
    public IEndpointDefinition<TFuture>? EndpointDefinition { get; set; }

    IEndpointDefinition? IFutureDefinition.EndpointDefinition => EndpointDefinition;

    /// <summary>Gets or sets the maximum number of future messages processed concurrently by the endpoint.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        protected set
        {
            if (value is <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The concurrent message limit must be positive.");

            _concurrentMessageLimit = value;
        }
    }

    void IFutureDefinition<TFuture>.Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(sagaConfigurator);
        ArgumentNullException.ThrowIfNull(context);
        if (_concurrentMessageLimit.HasValue)
            sagaConfigurator.ConcurrentMessageLimit = _concurrentMessageLimit;
        ConfigureSaga(endpointConfigurator, sagaConfigurator, context);
    }

    Type IFutureDefinition.FutureType => typeof(TFuture);

    string IFutureDefinition.GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        return string.IsNullOrWhiteSpace(_endpointName)
            ? _endpointName = EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Message<TFuture>()
            : _endpointName!;
    }

    /// <summary>Applies future-specific saga configuration after the endpoint has been created.</summary>
    /// <param name="endpointConfigurator">The configurator for the hosting receive endpoint.</param>
    /// <param name="sagaConfigurator">The configurator for persisted future state.</param>
    /// <param name="context">The registration context that resolves configuration dependencies.</param>
    protected virtual void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context)
    {
    }

    /// <summary>Defines endpoint settings for the future.</summary>
    /// <param name="configure">The optional callback that customizes endpoint settings.</param>
    protected void Endpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<TFuture>();

        configure?.Invoke(configurator);

        EndpointDefinition = new FutureEndpointDefinition<TFuture>(configurator.Settings);
    }
}
