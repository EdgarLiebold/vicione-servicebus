using System;
namespace ViciOne.ServiceBus.Middleware.Outbox.InMemory;

/// <summary>Identifies one consumed message within one logical consumer inbox.</summary>
/// <param name="MessageId">The identifier of the incoming message.</param>
/// <param name="ConsumerId">The stable identifier of the logical consumer.</param>
internal readonly record struct InMemoryInboxMessageKey(Guid MessageId, Guid ConsumerId);
