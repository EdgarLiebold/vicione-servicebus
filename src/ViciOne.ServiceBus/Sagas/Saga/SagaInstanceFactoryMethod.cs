using System;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Represents the method that handles saga instance factory method.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <param name="correlationId">The correlation id value.</param>
/// <returns>The result of the operation.</returns>
public delegate TSaga SagaInstanceFactoryMethod<out TSaga>(Guid correlationId)
    where TSaga : class, ISaga;
