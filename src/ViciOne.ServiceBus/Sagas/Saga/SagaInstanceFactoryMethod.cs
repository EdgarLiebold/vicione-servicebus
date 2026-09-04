using System;

namespace ViciOne.ServiceBus.Saga;

public delegate TSaga SagaInstanceFactoryMethod<out TSaga>(Guid correlationId)
    where TSaga : class, ISaga;
