using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Payloads;

/// <summary>Stores strongly typed objects associated with a pipe context.</summary>
public interface IPayloadCache
{
    /// <summary>Determines whether a payload assignable to <paramref name="payloadType" /> is available.</summary>
    /// <param name="payloadType">The requested payload type.</param>
    /// <returns><see langword="true" /> when a compatible payload is available; otherwise, <see langword="false" />.</returns>
    bool HasPayloadType(Type payloadType);

    /// <summary>Tries to get the most recently stored compatible payload.</summary>
    /// <typeparam name="TPayload">The requested payload type.</typeparam>
    /// <param name="payload">The compatible payload when one is available.</param>
    /// <returns><see langword="true" /> when a compatible payload was found; otherwise, <see langword="false" />.</returns>
    bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class;

    /// <summary>Gets an existing compatible payload or atomically creates and stores one.</summary>
    /// <typeparam name="TPayload">The requested payload type.</typeparam>
    /// <param name="payloadFactory">The factory used when no compatible payload exists.</param>
    /// <returns>The existing or newly created payload.</returns>
    TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class;

    /// <summary>Atomically creates a payload or replaces the most recent compatible payload.</summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="addFactory">The factory used when no compatible payload exists.</param>
    /// <param name="updateFactory">The factory used to replace an existing compatible payload.</param>
    /// <returns>The newly created or updated payload.</returns>
    TPayload AddOrUpdatePayload<TPayload>(PayloadFactory<TPayload> addFactory, UpdatePayloadFactory<TPayload> updateFactory)
        where TPayload : class;
}
