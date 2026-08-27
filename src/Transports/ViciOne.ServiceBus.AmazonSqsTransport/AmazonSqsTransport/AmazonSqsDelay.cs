namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;


static class AmazonSqsDelay
{
    internal const int MaximumSeconds = 15 * 60;

    public static int FromTimeSpan(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero || delay > TimeSpan.FromSeconds(MaximumSeconds))
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "An SQS message delay must be between 0 and 900 seconds.");

        // The service accepts only whole seconds, while the high-level scheduler supplies an
        // absolute timestamp and therefore reaches the transport with a fractional remainder.
        // Round up so a scheduled message is never exposed before its requested not-before time.
        return checked((int)Math.Ceiling(delay.TotalSeconds));
    }

    public static int? ForQueue(TimeSpan? delay, bool isFifo)
    {
        if (!delay.HasValue || delay.Value == TimeSpan.Zero)
            return null;

        var seconds = FromTimeSpan(delay.Value);
        if (isFifo)
            throw new NotSupportedException("Amazon SQS FIFO queues do not support per-message delay. Configure delay on the queue instead.");

        return seconds;
    }

    public static void EnsureNotSetForTopic(TimeSpan? delay)
    {
        if (delay.HasValue && delay.Value != TimeSpan.Zero)
            throw new NotSupportedException("Amazon SNS topics do not support per-message delivery delay.");
    }
}
