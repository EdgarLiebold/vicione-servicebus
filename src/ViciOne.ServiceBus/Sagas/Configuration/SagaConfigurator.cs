using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga configurator implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaConfigurator<TSaga> :
    ISagaConfigurator<TSaga>,
    IReceiveEndpointSpecification
    where TSaga : class, ISaga
{
    readonly ISagaRepository<TSaga> _sagaRepository;
    readonly ISagaSpecification<TSaga> _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagaRepository">The saga repository value.</param>
    /// <param name="observer">The observer value.</param>
    public SagaConfigurator(ISagaRepository<TSaga> sagaRepository, ISagaConfigurationObserver observer)
    {
        _sagaRepository = sagaRepository ?? throw new ArgumentNullException(nameof(sagaRepository));
        ArgumentNullException.ThrowIfNull(observer);

        _specification = SagaConnectorCache<TSaga>.Connector.CreateSagaSpecification<TSaga>();

        _specification.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        SagaConnectorCache<TSaga>.Connector.ConnectSaga(builder, _sagaRepository, _specification);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specification.Validate();
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
        where T : class
    {
        _specification.Message(configure);
    }

    /// <summary>
    /// Performs the saga message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
        where T : class
    {
        _specification.SagaMessage(configure);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
    {
        _specification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Connects saga configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer)
    {
        return _specification.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public T Options<T>(Action<T>? configure)
        where T : IOptions, new()
    {
        return _specification.Options(configure);
    }

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public T Options<T>(T options, Action<T>? configure)
        where T : IOptions
    {
        return _specification.Options(options, configure);
    }

    /// <summary>
    /// Attempts to get options.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        return _specification.TryGetOptions(out options);
    }

    /// <summary>
    /// Performs the select options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<T> SelectOptions<T>()
        where T : class
    {
        return _specification.SelectOptions<T>();
    }
}
