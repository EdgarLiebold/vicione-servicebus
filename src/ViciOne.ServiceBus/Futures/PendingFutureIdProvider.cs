using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Represents the method that handles pending future id provider.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="message">The message to process.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Guid PendingFutureIdProvider<in T>(T message)
    where T : class;
