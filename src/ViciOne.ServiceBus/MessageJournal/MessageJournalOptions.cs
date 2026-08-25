#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

using System;
using System.Threading;

/// <summary>
/// Immutable runtime limits for an explicitly connected message journal.
/// </summary>
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

    public TimeSpan WriteTimeout { get; }

    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Explicitly selects the only safe observer failure contract: journal failures are observable
    /// but never alter the message operation that has already succeeded or faulted.
    /// </summary>
    public static MessageJournalOptions ContinueMessageFlow(TimeSpan writeTimeout, TimeProvider timeProvider)
    {
        return new MessageJournalOptions(writeTimeout, timeProvider);
    }
}
