using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Payloads;

/// <summary>Caches list payload data.</summary>
public class ListPayloadCache :
    IPayloadCache
{
    IList<object>? _cache;

    /// <summary>Initializes a new instance.</summary>
    public ListPayloadCache()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="payloads">The payloads.</param>
    public ListPayloadCache(object[] payloads)
    {
        _cache = new List<object>(payloads);
    }

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPayloadType(Type payloadType)
    {
        if (_cache == null)
            return false;

        lock (this)
        {
            for (var i = _cache.Count - 1; i >= 0; i--)
            {
                if (payloadType.IsInstanceOfType(_cache[i]))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class
    {
        if (_cache == null)
        {
            payload = default;
            return false;
        }

        lock (this)
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

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        lock (this)
        {
            if (_cache != null)
            {
                for (var i = _cache.Count - 1; i >= 0; i--)
                {
                    if (_cache[i] is T result)
                        return result;
                }
            }

            var payload = payloadFactory();

            if (_cache != null)
                _cache.Add(payload);
            else
                _cache = new List<object>(1) { payload };

            return payload;
        }
    }

    /// <summary>Adds or update payload to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="addFactory">The add factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <returns>The t produced by the operation.</returns>
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        lock (this)
        {
            if (_cache != null)
            {
                for (var i = _cache.Count - 1; i >= 0; i--)
                {
                    if (_cache[i] is T result)
                    {
                        var updated = updateFactory(result);

                        _cache[i] = updated;

                        return updated;
                    }
                }
            }

            var payload = addFactory();

            if (_cache != null)
                _cache.Add(payload);
            else
                _cache = new List<object>(1) { payload };

            return payload;
        }
    }
}
