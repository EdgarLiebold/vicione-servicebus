using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides cancellation and typed supplemental payloads to a pipeline operation.</summary>
public interface PipeContext
{
    /// <summary>Gets the token that cancels the pipeline operation.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Determines whether a payload assignable to a runtime type is available.</summary>
    /// <param name="payloadType">The payload type to query.</param>
    /// <returns><see langword="true" /> when a compatible payload is available; otherwise, <see langword="false" />.</returns>
    bool HasPayloadType(Type payloadType);

    /// <summary>Tries to retrieve a compatible payload.</summary>
    /// <typeparam name="TPayload">The requested payload type.</typeparam>
    /// <param name="payload">The payload when one is available.</param>
    /// <returns><see langword="true" /> when a compatible payload is available; otherwise, <see langword="false" />.</returns>
    bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class;

    /// <summary>Returns a compatible payload or creates and stores one when none exists.</summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="payloadFactory">The factory used when no compatible payload exists.</param>
    /// <returns>The existing or newly created payload.</returns>
    TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class;

    /// <summary>Creates a payload when absent or replaces a compatible existing payload.</summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="addFactory">The factory used when no compatible payload exists.</param>
    /// <param name="updateFactory">The factory used to replace an existing payload.</param>
    /// <returns>The newly created or updated payload.</returns>
    TPayload AddOrUpdatePayload<TPayload>(PayloadFactory<TPayload> addFactory, UpdatePayloadFactory<TPayload> updateFactory)
        where TPayload : class;
}
