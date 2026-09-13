using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Selects the identifier of a pending future operation from its message.</summary>
/// <typeparam name="T">The operation message type.</typeparam>
/// <param name="message">The message to process.</param>
/// <returns>The pending operation identifier.</returns>
public delegate Guid PendingFutureIdProvider<in T>(T message)
    where T : class;
