using System;
using System.Linq;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Sagas;

static class SagaRegistrationMetadata
{
    public static bool IsSagaOrDefinition(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type[] interfaces = type.GetInterfaces();
        return interfaces.Contains(typeof(ISaga))
            || interfaces.Any(candidate => candidate.ImplementsInterface(typeof(IInitiatedBy<>))
                || candidate.ImplementsInterface(typeof(IOrchestrates<>))
                || candidate.ImplementsInterface(typeof(IInitiatedByOrOrchestrates<>))
                || candidate.ImplementsInterface(typeof(IObserves<,>))
                || candidate.ImplementsInterface(typeof(ISagaDefinition<>)));
    }

    public static bool IsSagaStateMachineOrDefinition(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type[] interfaces = type.GetInterfaces();
        return interfaces.Any(candidate => IsOwnedStateMachine(candidate)
            || candidate.ImplementsInterface(typeof(ISagaDefinition<>))
                && !candidate.IsDefined(typeof(ConsumerRegistrationExclusionAttribute), inherit: false));
    }

    public static bool IsConsumerKindOwned(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type[] interfaces = type.GetInterfaces();
        return interfaces.Any(candidate => candidate.IsDefined(typeof(ConsumerRegistrationExclusionAttribute), inherit: false))
            || interfaces.Any(candidate => candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(ISagaStateMachine<>)
                && typeof(IConsumerKindOwnedState).IsAssignableFrom(candidate.GetGenericArguments()[0]));
    }

    static bool IsOwnedStateMachine(Type candidate)
    {
        if (!candidate.IsGenericType || candidate.GetGenericTypeDefinition() != typeof(ISagaStateMachine<>))
            return false;

        return !typeof(IConsumerKindOwnedState).IsAssignableFrom(candidate.GetGenericArguments()[0]);
    }
}
