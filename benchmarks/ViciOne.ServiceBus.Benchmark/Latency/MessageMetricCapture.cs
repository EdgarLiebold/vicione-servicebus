namespace ViciOneServiceBusBenchmark.Latency
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Util;


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
        readonly Stopwatch _stopwatch;
        long _consumed;
        long _sent;

        public MessageMetricCapture(long messageCount)
        {
            _messageCount = messageCount;

            _consumedMessages = new ConcurrentBag<ConsumedMessage>();
            _sentMessages = new ConcurrentDictionary<Guid, SentMessage>();
            _sendCompleted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
            _consumeCompleted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);

            _stopwatch = Stopwatch.StartNew();
        }

        public Task<TimeSpan> SendCompleted => _sendCompleted.Task;
        public Task<TimeSpan> ConsumeCompleted => _consumeCompleted.Task;

        Task IReportConsumerMetric.Consumed<T>(Guid messageId)
        {
            _consumedMessages.Add(new ConsumedMessage(messageId, _stopwatch.ElapsedTicks));

            var consumed = Interlocked.Increment(ref _consumed);
            if (consumed == _messageCount)
                _consumeCompleted.TrySetResult(_stopwatch.Elapsed);

            return TaskUtil.Completed;
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

            if (!_sentMessages.TryAdd(messageId, new SentMessage(_stopwatch.ElapsedTicks)))
                throw new InvalidOperationException($"The message {messageId} was already registered as sent.");

            try
            {
                await send().ConfigureAwait(false);
            }
            catch
            {
                _sentMessages.TryRemove(messageId, out _);

                throw;
            }

            if (postSend)
                return;

            Complete(messageId, _stopwatch.ElapsedTicks);
        }

        /// <summary>
        /// Closes the measurement a transport send observer reports. Taken before anything else, so the
        /// bookkeeping below cannot be mistaken for transport time.
        /// </summary>
        public Task PostSend(Guid messageId)
        {
            Complete(messageId, _stopwatch.ElapsedTicks);

            return TaskUtil.Completed;
        }

        /// <summary>
        /// Closes exactly one registered send exactly once.
        /// <para>
        /// An unknown message is an error rather than a new entry: it means the completion belongs to a
        /// send this capture never saw, and inventing a start for it would report a latency that was
        /// never measured. A repeated completion is ignored instead, because a transport may legitimately
        /// observe the same send twice, and counting it twice would end the run before every message had
        /// actually been sent.
        /// </para>
        /// </summary>
        void Complete(Guid messageId, long completionTimestamp)
        {
            if (!_sentMessages.TryGetValue(messageId, out var message))
                throw new InvalidOperationException(
                    $"The message {messageId} was completed without ever being registered as sent.");

            if (!message.TryComplete(completionTimestamp))
                return;

            var sent = Interlocked.Increment(ref _sent);
            if (sent == _messageCount)
                _sendCompleted.TrySetResult(_stopwatch.Elapsed);
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
            /// <summary>
            /// No stopwatch reading can be negative, so this cannot collide with a real timestamp.
            /// </summary>
            const long NotCompleted = long.MinValue;

            public readonly long SendTimestamp;
            long _completionTimestamp = NotCompleted;

            public SentMessage(long sendTimestamp)
            {
                SendTimestamp = sendTimestamp;
            }

            public bool IsCompleted => Volatile.Read(ref _completionTimestamp) != NotCompleted;
            public long CompletionTimestamp => Volatile.Read(ref _completionTimestamp);

            /// <summary>
            /// Completion is the timestamp itself, written by one compare and exchange, so a reader can
            /// never see a message marked complete before the value it was completed with is there.
            /// </summary>
            public bool TryComplete(long completionTimestamp)
            {
                return Interlocked.CompareExchange(ref _completionTimestamp, completionTimestamp, NotCompleted)
                    == NotCompleted;
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
}
