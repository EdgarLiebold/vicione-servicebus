using ViciOne.ServiceBus.Operations;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Validates the composite seek contract shared by reliable inbox persistence providers.</summary>
public static class ReliableInboxQuarantinePagination
{
    /// <summary>Validates page-size and all-or-none cursor invariants.</summary>
    /// <param name="query">The inbox quarantine query.</param>
    /// <returns>The supplied validated query.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Only part of the composite cursor is present.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The page size is outside the published finite range.</exception>
    public static ReliableInboxQuarantineQuery Validate(ReliableInboxQuarantineQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.Validate();
    }
}
