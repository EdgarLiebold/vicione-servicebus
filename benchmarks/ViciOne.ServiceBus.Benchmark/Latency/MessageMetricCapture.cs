using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOneServiceBusBenchmark.Latency;

/// <summary>
/// Records one send and one consume timestamp per message.
/// <para>
/// Two modes, and the difference between them is the whole reason this class is careful. In the
/// direct mode the send is finished when the delegate returns, so the same call can open and close
/// the measurement. In the postSend mode — the bus outbox — the transport's own send observer
/// reports completion, and it does so from inside the send delegate, before that delegate returns.
/// </para>
/// <para>
/// So the start has to be registered before the delegate is invoked rather than after it returns.
/// Registering afterwards meant the observer's completion arrived first, found nothing to attach
/// itself to, and created an entry whose send timestamp was zero: that message was then reported as
/// having taken everything since the benchmark started, and the figure looked like a transport
/// result rather than the bookkeeping error it was.
/// </para>
/// </summary>
public class MessageMetricCapture :
    IReportConsumerMetric
{
    readonly TaskCompletionSource<TimeSpan> _consumeCompleted;
    readonly ConcurrentBag<ConsumedMessage> _consumedMessages;
    readonly long _messageCount;
    readonly TaskCompletionSource<TimeSpan> _sendCompleted;
    readonly ConcurrentDictionary<Guid, SentMessage> _sentMessages;
    readonly IBenchmarkMetricClock _clock;
    long _consumed;
    long _sent;

    public MessageMetricCapture(long messageCount)
        : this(messageCount, new StopwatchBenchmarkMetricClock())
    {
    }

    internal MessageMetricCapture(long messageCount, IBenchmarkMetricClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _messageCount = messageCount;

        _consumedMessages = new ConcurrentBag<ConsumedMessage>();
        _sentMessages = new ConcurrentDictionary<Guid, SentMessage>();
        _sendCompleted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
        _consumeCompleted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task<TimeSpan> SendCompleted => _sendCompleted.Task;
    public Task<TimeSpan> ConsumeCompleted => _consumeCompleted.Task;

    Task IReportConsumerMetric.Consumed<T>(Guid messageId)
    {
        _consumedMessages.Add(new ConsumedMessage(messageId, _clock.ElapsedTicks));

        var consumed = Interlocked.Increment(ref _consumed);
        if (consumed == _messageCount)
            _consumeCompleted.TrySetResult(_clock.Elapsed);

        return TaskResults.Completed;
    }

    /// <summary>
    /// Registers the send, runs it, and — outside the postSend mode — closes the measurement itself.
    /// A send that throws takes its registration back with it, so a failure leaves no half message
    /// behind for a later completion to find.
    /// </summary>
    public async Task Sent(Guid messageId, Func<Task> send, bool postSend = false)
    {
        if (send == null)
            throw new ArgumentNullException(nameof(send));

        var message = new SentMessage(_clock.ElapsedTicks);
        if (!_sentMessages.TryAdd(messageId, message))
            throw new InvalidOperationException($"The message {messageId} was already registered as sent.");

        try
        {
            await send().ConfigureAwait(false);
        }
        catch
        {
            // Terminal. The state goes with the failure, and because a success is only ever
            // published once both facts hold, a completion the observer already reported for this
            // message never became a count that would now have to be taken back.
            _sentMessages.TryRemove(messageId, out _);

            throw;
        }

        if (!postSend)
            message.TryObserveCompletion(_clock.ElapsedTicks);

        if (message.TrySendReturned())
            PublishSend();
    }

    /// <summary>
    /// Closes the measurement a transport send observer reports. The timestamp is taken before
    /// anything else, so the bookkeeping below cannot be mistaken for transport time.
    /// </summary>
    public Task PostSend(Guid messageId)
    {
        Complete(messageId, _clock.ElapsedTicks);

        return TaskResults.Completed;
    }

    /// <summary>
    /// Records the completion a transport send observer reports.
    /// <para>
    /// A send is successful only when both facts hold: the observer reported completion and the
    /// send delegate returned without throwing. Counting on the observer alone was wrong, because
    /// the observer fires from inside the delegate: a send that reported completion and then threw
    /// was counted as delivered, and a run could reach its expected total before every message had
    /// actually been sent. Whichever of the two facts arrives second publishes the success, exactly
    /// once, so nothing has to be counted back afterwards.
    /// </para>
    /// </summary>
    void Complete(Guid messageId, long completionTimestamp)
    {
        if (!_sentMessages.TryGetValue(messageId, out var message))
            throw new InvalidOperationException(
                $"The message {messageId} was completed without ever being registered as sent.");

        if (message.TryObserveCompletion(completionTimestamp))
            PublishSend();
    }

    /// <summary>
    /// Publishes one confirmed send. Only ever reached by the side that closed the pair.
    /// </summary>
    void PublishSend()
    {
        var sent = Interlocked.Increment(ref _sent);
        if (sent == _messageCount)
            _sendCompleted.TrySetResult(_clock.Elapsed);
    }

    public MessageMetric[] GetMessageMetrics()
    {
        return _sentMessages
            .Where(entry => entry.Value.IsCompleted)
            .Join(_consumedMessages, x => x.Key, x => x.MessageId, (sent, consumed) =>
                new MessageMetric(sent.Key, sent.Value.CompletionTimestamp - sent.Value.SendTimestamp,
                    consumed.Timestamp - sent.Value.SendTimestamp))
            .ToArray();
    }


    /// <summary>
    /// One registered send. A class rather than a struct because completion has to be a single
    /// atomic act observable by every caller, and a struct in a dictionary is copied on every read.
    /// </summary>
    sealed class SentMessage
    {
        /// <summary>No stopwatch reading can be negative, so this cannot collide with a real one.</summary>
        const long NotCompleted = long.MinValue;

        public readonly long SendTimestamp;
        long _completionTimestamp = NotCompleted;
        int _sendReturned;
        int _counted;

        public SentMessage(long sendTimestamp)
        {
            SendTimestamp = sendTimestamp;
        }

        public bool IsCompleted =>
            Volatile.Read(ref _completionTimestamp) != NotCompleted && Volatile.Read(ref _sendReturned) != 0;

        public long CompletionTimestamp => Volatile.Read(ref _completionTimestamp);

        /// <summary>
        /// The observer reported completion. True only for the call that closes the pair, so a
        /// repeated or concurrent report never counts twice.
        /// </summary>
        public bool TryObserveCompletion(long completionTimestamp)
        {
            if (Interlocked.CompareExchange(ref _completionTimestamp, completionTimestamp, NotCompleted)
                != NotCompleted)
                return false;

            return Volatile.Read(ref _sendReturned) != 0 && TryClaim();
        }

        /// <summary>The send delegate returned without throwing. True only for the call that closes the pair.</summary>
        public bool TrySendReturned()
        {
            if (Interlocked.Exchange(ref _sendReturned, 1) != 0)
                return false;

            return Volatile.Read(ref _completionTimestamp) != NotCompleted && TryClaim();
        }

        /// <summary>
        /// Both sides may observe the other's fact at once. The exchange decides which of them
        /// publishes, so the pair is counted exactly once in either order.
        /// </summary>
        bool TryClaim()
        {
            return Interlocked.CompareExchange(ref _counted, 1, 0) == 0;
        }
    }


    struct ConsumedMessage
    {
        public readonly Guid MessageId;
        public readonly long Timestamp;

        public ConsumedMessage(Guid messageId, long timestamp)
        {
            MessageId = messageId;
            Timestamp = timestamp;
        }
    }
}
