using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Represents the method that handles pending future id provider.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="message">The message value.</param>
/// <returns>The result of the operation.</returns>
public delegate Guid PendingFutureIdProvider<in T>(T message)
    where T : class;
