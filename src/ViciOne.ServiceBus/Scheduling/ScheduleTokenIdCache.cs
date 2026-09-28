using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Stores the process-wide scheduling-token selector for a message contract.</summary>
/// <typeparam name="T">The message contract.</typeparam>
internal sealed class ScheduleTokenIdCache<T>
    where T : class
{
    readonly Func<T, Guid?> _selector;

    ScheduleTokenIdCache(Func<T, Guid?> selector)
    {
        _selector = selector;
    }

    ScheduleTokenIdCache()
    {
        _selector = x => default;
    }

    bool TryGetTokenId(T message, out Guid tokenId)
    {
        Guid? result = _selector(message);
        if (result.HasValue)
        {
            if (result.Value == Guid.Empty)
                throw new ArgumentException("The selected scheduling token cannot be empty.", nameof(message));

            tokenId = result.Value;
            return true;
        }

        tokenId = default;
        return false;
    }

    internal static Guid GetTokenId(T message, Guid? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (Cached.Metadata.Value.TryGetTokenId(message, out var tokenId))
            return tokenId;

        return defaultValue ?? NewId.NextGuid();
    }

    internal static void UseTokenId(Func<T, Guid?> tokenIdSelector)
    {
        ArgumentNullException.ThrowIfNull(tokenIdSelector);

        lock (Cached.Gate)
        {
            if (Cached.IsConfigured || Cached.Metadata.IsValueCreated)
                return;

            Cached.Metadata = new Lazy<ScheduleTokenIdCache<T>>(() => new ScheduleTokenIdCache<T>(tokenIdSelector));
            Cached.IsConfigured = true;
        }
    }


    static class Cached
    {
        internal static readonly object Gate = new();
        internal static bool IsConfigured;
        internal static Lazy<ScheduleTokenIdCache<T>> Metadata = new(() => new ScheduleTokenIdCache<T>());
    }
}
