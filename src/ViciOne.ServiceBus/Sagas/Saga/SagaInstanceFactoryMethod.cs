using System;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Represents the method that handles saga instance factory method.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="correlationId">The correlation id.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TSaga SagaInstanceFactoryMethod<out TSaga>(Guid correlationId)
    where TSaga : class, ISaga;
