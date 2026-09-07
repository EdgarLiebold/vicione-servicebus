using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaConfigurator<TSaga> :
    ISagaConfigurator<TSaga>,
    IReceiveEndpointSpecification
    where TSaga : class, ISaga
{
    readonly ISagaRepository<TSaga> _sagaRepository;
    readonly ISagaSpecification<TSaga> _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagaRepository">The saga repository.</param>
    /// <param name="observer">The observer to connect.</param>
    public SagaConfigurator(ISagaRepository<TSaga> sagaRepository, ISagaConfigurationObserver observer)
    {
        _sagaRepository = sagaRepository ?? throw new ArgumentNullException(nameof(sagaRepository));
        ArgumentNullException.ThrowIfNull(observer);

        _specification = SagaConnectorCache<TSaga>.Connector.CreateSagaSpecification<TSaga>();

        _specification.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        SagaConnectorCache<TSaga>.Connector.ConnectSaga(builder, _sagaRepository, _specification);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specification.Validate();
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
        where T : class
    {
        _specification.Message(configure);
    }

    /// <summary>Configures the saga message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
        where T : class
    {
        _specification.SagaMessage(configure);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
    {
        _specification.AddPipeSpecification(specification);
    }

    /// <summary>Connects saga configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return _specification.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Options<T>(Action<T>? configure)
        where T : IOptions, new()
    {
        return _specification.Options(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Options<T>(T options, Action<T>? configure)
        where T : IOptions
    {
        return _specification.Options(options, configure);
    }

    /// <summary>Attempts to get options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">Receives the options produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        return _specification.TryGetOptions(out options);
    }

    /// <summary>Selects options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The selected options.</returns>
    public IEnumerable<T> SelectOptions<T>()
        where T : class
    {
        return _specification.SelectOptions<T>();
    }
}
