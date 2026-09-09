using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Payloads;

/// <summary>Stores pipe-context payloads in insertion order and resolves the most recently added compatible value.</summary>
public sealed class ListPayloadCache :
    IPayloadCache
{
    readonly List<object> _cache;
    readonly object _syncRoot = new();

    /// <summary>Initializes a new instance.</summary>
    public ListPayloadCache()
    {
        _cache = [];
    }

    /// <summary>Initializes the cache with the supplied payloads.</summary>
    /// <param name="payloads">The payloads to store in their supplied order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="payloads" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="payloads" /> contains a <see langword="null" /> element.</exception>
    public ListPayloadCache(object[] payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);

        if (Array.IndexOf(payloads, null) >= 0)
            throw new ArgumentException("Payloads cannot contain null elements.", nameof(payloads));

        _cache = new List<object>(payloads);
    }

    /// <inheritdoc />
    public bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);

        lock (_syncRoot)
        {
            for (var i = _cache.Count - 1; i >= 0; i--)
            {
                if (payloadType.IsInstanceOfType(_cache[i]))
                    return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        lock (_syncRoot)
        {
            for (var i = _cache.Count - 1; i >= 0; i--)
            {
                if (_cache[i] is TPayload p)
                {
                    payload = p;
                    return true;
                }
            }
        }

        payload = default;
        return false;
    }

    /// <inheritdoc />
    public TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        lock (_syncRoot)
        {
            for (var i = _cache.Count - 1; i >= 0; i--)
            {
                if (_cache[i] is TPayload result)
                    return result;
            }

            TPayload payload = payloadFactory()
                ?? throw new InvalidOperationException("The payload factory returned null.");

            _cache.Add(payload);

            return payload;
        }
    }

    /// <inheritdoc />
    public TPayload AddOrUpdatePayload<TPayload>(PayloadFactory<TPayload> addFactory, UpdatePayloadFactory<TPayload> updateFactory)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        lock (_syncRoot)
        {
            for (var i = _cache.Count - 1; i >= 0; i--)
            {
                if (_cache[i] is TPayload result)
                {
                    TPayload updated = updateFactory(result)
                        ?? throw new InvalidOperationException("The payload update factory returned null.");

                    _cache[i] = updated;

                    return updated;
                }
            }

            TPayload payload = addFactory()
                ?? throw new InvalidOperationException("The payload factory returned null.");

            _cache.Add(payload);

            return payload;
        }
    }
}
