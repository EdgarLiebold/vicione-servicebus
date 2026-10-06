using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for saga.</summary>
public static class SagaExtensions
{
    /// <summary>Configure a saga subscription.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="sagaRepository">The saga repository.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void Saga<T>(this IReceiveEndpointConfigurator configurator, ISagaRepository<T> sagaRepository,
        Action<ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (sagaRepository == null)
            throw new ArgumentNullException(nameof(sagaRepository));

        try
        {
            LogContext.Debug?.Log("Subscribing Saga: {SagaType}", TypeCache<T>.ShortName);
        }
        catch (Exception)
        {
        }

        var sagaConfigurator = new SagaConfigurator<T>(sagaRepository, configurator);

        configure?.Invoke(sagaConfigurator);

        configurator.AddEndpointSpecification(sagaConfigurator);
    }

    /// <summary>Connects the saga to the bus.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <param name="connector">The bus to which the saga is to be connected.</param>
    /// <param name="sagaRepository">The saga repository.</param>
    /// <param name="pipeSpecifications">The pipe specifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectSaga<T>(this IConsumePipeConnector connector, ISagaRepository<T> sagaRepository,
        params IPipeSpecification<SagaConsumeContext<T>>[] pipeSpecifications)
        where T : class, ISaga
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));
        if (sagaRepository == null)
            throw new ArgumentNullException(nameof(sagaRepository));
        ArgumentNullException.ThrowIfNull(pipeSpecifications);
        foreach (IPipeSpecification<SagaConsumeContext<T>> pipeSpecification in pipeSpecifications)
            ArgumentNullException.ThrowIfNull(pipeSpecification);

        try
        {
            LogContext.Debug?.Log("Connecting Saga: {SagaType}", TypeCache<T>.ShortName);
        }
        catch (Exception)
        {
        }

        ISagaSpecification<T> specification = SagaConnectorCache<T>.Connector.CreateSagaSpecification<T>();
        foreach (IPipeSpecification<SagaConsumeContext<T>> pipeSpecification in pipeSpecifications)
            specification.AddPipeSpecification(pipeSpecification);

        return SagaConnectorCache<T>.Connector.ConnectSaga(connector, sagaRepository, specification);
    }
}
