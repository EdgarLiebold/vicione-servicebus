using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// A saga definition defines the configuration for a saga, which can be used by the automatic registration code to
/// configure the consumer on a receive endpoint.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaDefinition<TSaga> :
    ISagaDefinition<TSaga>
    where TSaga : class, ISaga
{
    int? _concurrentMessageLimit;
    string? _endpointName;

    /// <summary>Initializes a new instance.</summary>
    protected SagaDefinition()
    {
    }

    /// <summary>
    /// Specify the endpoint name (which may be a queue, or a subscription, depending upon the transport) on which the saga
    /// should be configured.
    /// </summary>
    protected string EndpointName
    {
        set => _endpointName = value;
    }

    /// <summary>Gets or sets the endpoint definition.</summary>
    public IEndpointDefinition<TSaga>? EndpointDefinition { get; set; }

    IEndpointDefinition? ISagaDefinition.EndpointDefinition => EndpointDefinition;

    /// <summary>
    /// Set the concurrent message limit for the saga, which limits how many saga instances are able to concurrently
    /// consume messages.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        protected set
        {
            if (value is <= 0)
                throw new ArgumentOutOfRangeException(nameof(ConcurrentMessageLimit), value,
                    "The concurrent message limit must be greater than zero.");

            _concurrentMessageLimit = value;
        }
    }

    void ISagaDefinition<TSaga>.Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<TSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(sagaConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        if (_concurrentMessageLimit.HasValue)
            sagaConfigurator.ConcurrentMessageLimit = _concurrentMessageLimit;
        ConfigureSaga(endpointConfigurator, sagaConfigurator, context);
    }

    Type ISagaDefinition.SagaType => typeof(TSaga);

    string ISagaDefinition.GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return string.IsNullOrWhiteSpace(_endpointName)
            ? EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Saga<TSaga>()
            : _endpointName!;
    }

    /// <summary>
    /// Called when configuring the saga on the endpoint. Configuration only applies to this saga, and does not apply to
    /// the endpoint.
    /// </summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="sagaConfigurator">The saga configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected virtual void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<TSaga> sagaConfigurator,
        IRegistrationContext context)
    {
    }

    /// <summary>Configure the saga endpoint.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    protected void Endpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<TSaga>();

        configure?.Invoke(configurator);

        EndpointDefinition = new SagaEndpointDefinition<TSaga>(configurator.Settings);
    }
}
