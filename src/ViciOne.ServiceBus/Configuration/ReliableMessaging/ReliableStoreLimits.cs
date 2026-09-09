namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines hard retained-storage limits shared by outbox, inbox quarantine and schedules.</summary>
public sealed record ReliableStoreLimits
{
    /// <summary>Gets or initializes the maximum retained record count.</summary>
    public int MaximumStoredCount { get; init; }

    /// <summary>Gets or initializes the maximum retained logical content bytes.</summary>
    public long MaximumStoredBytes { get; init; }
}
