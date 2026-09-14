using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>Immutable runtime limits for an explicitly connected message journal.</summary>
public sealed class MessageJournalOptions
{
    private static readonly TimeSpan MaximumTimerDelay = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

    private MessageJournalOptions(TimeSpan writeTimeout, TimeProvider timeProvider)
    {
        if (writeTimeout <= TimeSpan.Zero || writeTimeout > MaximumTimerDelay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(writeTimeout),
                writeTimeout,
                $"The message-journal write timeout must be greater than zero and no longer than {MaximumTimerDelay:c}.");
        }

        WriteTimeout = writeTimeout;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets the maximum duration allowed for processing one journal observation.</summary>
    public TimeSpan WriteTimeout { get; }

    /// <summary>Gets the clock used for observation timestamps, elapsed time, and write-timeout scheduling.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Explicitly selects the only safe observer failure contract: journal failures are observable
    /// but never alter the message operation that has already succeeded or faulted.
    /// </summary>
    /// <param name="writeTimeout">The finite, positive limit for processing one observation.</param>
    /// <param name="timeProvider">The clock used for timestamps, elapsed time, and timeout scheduling.</param>
    /// <returns>Immutable options that isolate journal failures from the observed message flow.</returns>
    public static MessageJournalOptions ContinueMessageFlow(TimeSpan writeTimeout, TimeProvider timeProvider)
    {
        return new MessageJournalOptions(writeTimeout, timeProvider);
    }
}
