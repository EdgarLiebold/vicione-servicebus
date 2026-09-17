using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides typed and runtime saga registration extensions.</summary>
public static class SagaRegistrationConfiguratorRuntimeExtensions
{
    /// <summary>Adds the saga, along with an optional saga definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="sagaType">The saga type.</param>
    /// <param name="sagaDefinitionType">The saga definition type.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator AddSaga(this IRegistrationConfigurator configurator, Type sagaType, Type? sagaDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(sagaType);

        if (!IsClosedReferenceType(sagaType) || !typeof(ISaga).IsAssignableFrom(sagaType))
        {
            throw new ArgumentException(
                "The saga type must be a closed reference type that implements ISaga.",
                nameof(sagaType));
        }

        if (sagaType.ImplementsInterface<ISagaStateMachineInstance>())
        {
            throw new ArgumentException(
                $"State machine sagas must be registered using AddSagaStateMachine: {TypeCache.GetShortName(sagaType)}",
                nameof(sagaType));
        }

        var register = (IRegisterSaga)Activator.CreateInstance(typeof(RegisterSaga<>).MakeGenericType(sagaType))!;

        return register.Register(configurator, sagaDefinitionType);
    }

    /// <summary>Adds the state machine saga, along with an optional saga definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="sagaType">The saga type.</param>
    /// <param name="sagaDefinitionType">The saga definition type.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator AddSagaStateMachine(this IRegistrationConfigurator configurator, Type sagaType,
        Type? sagaDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(sagaType);

        IReadOnlyList<Type> stateMachineTypes = IsClosedReferenceType(sagaType)
            ? sagaType.GetClosedGenericTypes(typeof(ISagaStateMachine<>))
            : [];
        if (stateMachineTypes.Count != 1)
        {
            throw new ArgumentException(
                "The saga state machine type must be a closed reference type that implements exactly one "
                + "ISagaStateMachine<TSaga> whose state implements ISagaStateMachineInstance.",
                nameof(sagaType));
        }

        Type instanceType = stateMachineTypes[0].GetGenericArguments()[0];
        var register = (IRegisterSaga)Activator.CreateInstance(typeof(RegisterSagaStateMachine<,>).MakeGenericType(sagaType, instanceType))!;

        return register.Register(configurator, sagaDefinitionType);
    }

    static bool IsClosedReferenceType(Type type)
    {
        return !type.IsValueType && !type.IsByRef && !type.IsPointer && !type.ContainsGenericParameters;
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
        where TStateMachine : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        public ISagaRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? sagaDefinitionType)
        {
            return SagaRegistrationConfiguratorExtensions.AddSagaStateMachine<TStateMachine, TSaga>(configurator, sagaDefinitionType);
        }
    }
}
