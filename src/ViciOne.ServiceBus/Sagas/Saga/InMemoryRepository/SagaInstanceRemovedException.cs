using System;

namespace ViciOne.ServiceBus.Saga;

internal sealed class SagaInstanceRemovedException : InvalidOperationException
{
    public SagaInstanceRemovedException(Type sagaType, Guid correlationId)
        : base($"The saga instance was removed: {TypeCache.GetShortName(sagaType)}: {correlationId}")
    {
    }
}
