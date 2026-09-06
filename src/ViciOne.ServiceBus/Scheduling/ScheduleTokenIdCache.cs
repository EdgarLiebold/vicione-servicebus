using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>A cache of convention-based CorrelationId mappers, used unless overridden by some mystical force.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ScheduleTokenIdCache<T> :
    IScheduleTokenIdCache<T>
    where T : class
{
    /// <summary>Represents the method that handles token id selector.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate Guid? TokenIdSelector(T instance);


    readonly TokenIdSelector _selector;

    ScheduleTokenIdCache(TokenIdSelector selector)
    {
        _selector = selector;
    }

    ScheduleTokenIdCache()
    {
        _selector = x => default;
    }

    /// <summary>Attempts to get token id.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="tokenId">Receives the token id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetTokenId(T message, out Guid tokenId)
    {
        Guid? result = _selector(message);
        if (result.HasValue)
        {
            tokenId = result.Value;
            return true;
        }

        tokenId = default;
        return false;
    }

    /// <summary>Gets token id.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The token id.</returns>
    public static Guid GetTokenId(T message, Guid? defaultValue = default)
    {
        if (Cached.Metadata.Value.TryGetTokenId(message, out var tokenId))
            return tokenId;

        return defaultValue ?? NewId.NextGuid();
    }

    internal static void UseTokenId(TokenIdSelector tokenIdSelector)
    {
        if (Cached.Metadata.IsValueCreated)
            return;

        Cached.Metadata = new Lazy<IScheduleTokenIdCache<T>>(() => new ScheduleTokenIdCache<T>(tokenIdSelector));
    }


    static class Cached
    {
        internal static Lazy<IScheduleTokenIdCache<T>> Metadata = new Lazy<IScheduleTokenIdCache<T>>(() => new ScheduleTokenIdCache<T>());
    }
}
