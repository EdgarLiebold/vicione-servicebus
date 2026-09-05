using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides typed and runtime saga registration extensions.
/// </summary>
public static class SagaRegistrationConfiguratorRuntimeExtensions
{
    /// <summary>
    /// Adds the saga, along with an optional saga definition
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="sagaType">The saga type</param>
    /// <param name="sagaDefinitionType">The saga definition type</param>
    public static ISagaRegistrationConfigurator AddSaga(this IRegistrationConfigurator configurator, Type sagaType, Type? sagaDefinitionType = null)
    {
        if (sagaType.ImplementsInterface<SagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using AddSagaStateMachine: {TypeCache.GetShortName(sagaType)}");

        var register = (IRegisterSaga)(Activator.CreateInstance(typeof(RegisterSaga<>).MakeGenericType(sagaType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, sagaDefinitionType);
    }

    /// <summary>
    /// Adds the state machine saga, along with an optional saga definition
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="sagaType">The saga type</param>
    /// <param name="sagaDefinitionType">The saga definition type</param>
    public static ISagaRegistrationConfigurator AddSagaStateMachine(this IRegistrationConfigurator configurator, Type sagaType,
        Type? sagaDefinitionType = null)
    {
        if (!sagaType.TryGetSingleClosedGenericArguments(typeof(SagaStateMachine<>), out Type[] types))
            throw new ArgumentException($"The type is not a saga state machine: {TypeCache.GetShortName(sagaType)}", nameof(sagaType));

        var register = (IRegisterSaga)(Activator.CreateInstance(typeof(RegisterSagaStateMachine<,>).MakeGenericType(sagaType, types[0])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, sagaDefinitionType);
    }


    interface IRegisterSaga
    {
        ISagaRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? sagaDefinitionType);
    }


    class RegisterSaga<TSaga> :
        IRegisterSaga
        where TSaga : class, ISaga
    {
        public ISagaRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? sagaDefinitionType)
        {
            return SagaRegistrationConfiguratorExtensions.AddSaga<TSaga>(configurator, sagaDefinitionType);
        }
    }


    class RegisterSagaStateMachine<TStateMachine, TSaga> :
        IRegisterSaga
        where TStateMachine : class, SagaStateMachine<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        public ISagaRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? sagaDefinitionType)
        {
            return SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<TStateMachine, TSaga>(configurator, sagaDefinitionType);
        }
    }
}
