namespace ViciOne.ServiceBus;

/// <summary>Indicates that a concurrency-limit command predates the most recently applied command.</summary>
public sealed class StaleConcurrencyLimitCommandException :
    ViciOneServiceBusException,
    IRetryFailureClassification
{
    /// <summary>Creates a rejection for an out-of-order concurrency-limit command.</summary>
    /// <param name="commandTimestamp">The timestamp carried by the rejected command.</param>
    /// <param name="lastAppliedTimestamp">The timestamp of the most recently applied command.</param>
    public StaleConcurrencyLimitCommandException(
        DateTimeOffset commandTimestamp,
        DateTimeOffset lastAppliedTimestamp)
        : base(CreateMessage(commandTimestamp, lastAppliedTimestamp))
    {
        CommandTimestamp = commandTimestamp;
        LastAppliedTimestamp = lastAppliedTimestamp;
    }

    /// <summary>Gets the timestamp carried by the rejected command.</summary>
    public DateTimeOffset CommandTimestamp { get; }

    /// <summary>Gets the timestamp of the most recently applied command.</summary>
    public DateTimeOffset LastAppliedTimestamp { get; }

    RetryFailureKind IRetryFailureClassification.RetryFailureKind => RetryFailureKind.NonRetryable;

    private static string CreateMessage(DateTimeOffset commandTimestamp, DateTimeOffset lastAppliedTimestamp)
    {
        if (commandTimestamp >= lastAppliedTimestamp)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandTimestamp),
                commandTimestamp,
                $"The command timestamp must be earlier than {nameof(lastAppliedTimestamp)} ({lastAppliedTimestamp:O}).");
        }

        return $"The concurrency limit was updated after the command was sent. "
            + $"Command timestamp: {commandTimestamp:O}; last applied timestamp: {lastAppliedTimestamp:O}.";
    }
}
