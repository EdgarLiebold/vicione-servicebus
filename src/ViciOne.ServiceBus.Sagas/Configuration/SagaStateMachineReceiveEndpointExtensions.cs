using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for saga state machine receive endpoint.</summary>
public static class SagaStateMachineReceiveEndpointExtensions
{
    /// <summary>Subscribe a state machine saga to the endpoint.</summary>
    /// <typeparam name="TInstance">The state machine instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <param name="repository">The saga repository for the instances.</param>
    /// <param name="configure">Optionally configure the saga.</param>
    public static void StateMachineSaga<TInstance>(this IReceiveEndpointConfigurator configurator, SagaStateMachine<TInstance> stateMachine,
        ISagaRepository<TInstance> repository, Action<ISagaConfigurator<TInstance>>? configure = null)
        where TInstance : class, SagaStateMachineInstance
    {
        if (stateMachine == null)
            throw new ArgumentNullException(nameof(stateMachine));
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        var stateMachineConfigurator = new ViciOneServiceBusStateMachine<TInstance>.StateMachineSagaConfigurator(stateMachine, repository, configurator);

        configure?.Invoke(stateMachineConfigurator);

        configurator.AddEndpointSpecification(stateMachineConfigurator);
    }

    /// <summary>Connects state machine saga.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="bus">The bus.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectStateMachineSaga<TInstance>(this IConsumePipeConnector bus, SagaStateMachine<TInstance> stateMachine,
        ISagaRepository<TInstance> repository, Action<ISagaConfigurator<TInstance>>? configure = null)
        where TInstance : class, SagaStateMachineInstance
    {
        var connector = new ViciOneServiceBusStateMachine<TInstance>.StateMachineConnector(stateMachine);

        ISagaSpecification<TInstance> specification = connector.CreateSagaSpecification<TInstance>();

        configure?.Invoke(specification);

        return connector.ConnectSaga(bus, repository, specification);
    }
}
