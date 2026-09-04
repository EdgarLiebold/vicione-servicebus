using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

public static class RegistrationMetadata
{
    /// <summary>
    /// Returns true if the type is a consumer, or a consumer definition
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsConsumerOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return !IsSaga(type) && interfaces.Any(t => t.ImplementsInterface(typeof(IConsumer<>))
            || t.ImplementsInterface(typeof(IJobConsumer<>))
            || t.ImplementsInterface(typeof(IConsumerDefinition<>)));
    }

    /// <summary>
    /// Returns true if the type is a consumer, or a consumer definition
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsConsumer(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return interfaces.Any(t => t.ImplementsInterface(typeof(IConsumer<>))
            || t.ImplementsInterface(typeof(IJobConsumer<>)));
    }

    /// <summary>
    /// Returns true if the type is a saga, or a saga definition
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSagaOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        if (interfaces.Contains(typeof(ISaga)))
            return true;

        return interfaces.Any(t => t.ImplementsInterface(typeof(InitiatedBy<>))
            || t.ImplementsInterface(typeof(Orchestrates<>))
            || t.ImplementsInterface(typeof(InitiatedByOrOrchestrates<>))
            || t.ImplementsInterface(typeof(Observes<,>))
            || t.ImplementsInterface(typeof(ISagaDefinition<>)));
    }

    /// <summary>
    /// Returns true if the type is a saga
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSaga(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        if (interfaces.Contains(typeof(ISaga)))
            return true;

        return interfaces.Any(t => t.ImplementsInterface(typeof(InitiatedBy<>))
            || t.ImplementsInterface(typeof(Orchestrates<>))
            || t.ImplementsInterface(typeof(InitiatedByOrOrchestrates<>))
            || t.ImplementsInterface(typeof(Observes<,>)));
    }

    /// <summary>
    /// Returns true if the type is a state machine or saga definition
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSagaStateMachineOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return interfaces.Any(t => t.ImplementsInterface(typeof(SagaStateMachine<>))
            || t.ImplementsInterface(typeof(ISagaDefinition<>)));
    }

    /// <summary>
    /// Returns true if the type is an activity
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsActivityOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return interfaces.Any(t => t.ImplementsInterface(typeof(IExecuteActivity<>))
            || t.ImplementsInterface(typeof(ICompensateActivity<>))
            || t.ImplementsInterface(typeof(IActivityDefinition<,,>))
            || t.ImplementsInterface(typeof(IExecuteActivityDefinition<,>)));
    }

    /// <summary>
    /// Returns true if the type is a future or future definition
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsFutureOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return interfaces.Any(t => t.ImplementsInterface(typeof(SagaStateMachine<FutureState>))
            || t.ImplementsInterface(typeof(IFutureDefinition<>)));
    }
}
