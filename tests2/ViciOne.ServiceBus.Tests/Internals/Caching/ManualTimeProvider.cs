namespace ViciOne.ServiceBus.Tests.Internals.Caching;

internal sealed class ManualTimeProvider(long timestampFrequency) : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency { get; } = timestampFrequency > 0
        ? timestampFrequency
        : throw new ArgumentOutOfRangeException(nameof(timestampFrequency));

    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed));

        decimal exactTimestampUnits = (decimal)elapsed.Ticks * TimestampFrequency / TimeSpan.TicksPerSecond;
        if (exactTimestampUnits != decimal.Truncate(exactTimestampUnits))
        {
            throw new ArgumentException(
                "The elapsed value cannot be represented exactly by this timestamp frequency.",
                nameof(elapsed));
        }

        long elapsedTimestampUnits = checked((long)exactTimestampUnits);
        Interlocked.Add(ref _timestamp, elapsedTimestampUnits);
    }
}
