using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.Futures;

static class FutureRegistrationMetadata
{
    public static bool IsFutureOrDefinition(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetInterfaces().Any(candidate =>
            candidate.ImplementsInterface(typeof(SagaStateMachine<FutureState>))
            || candidate.ImplementsInterface(typeof(IFutureDefinition<>)));
    }
}
