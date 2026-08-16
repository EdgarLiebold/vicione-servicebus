namespace ViciOneServiceBusBenchmark.Latency
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Util;


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

        public async Task Sent(Guid messageId, Func<Task> send, bool postSend = false)
        {
            if (send == null)
                throw new ArgumentNullException(nameof(send));

            var sendTimestamp = _stopwatch.ElapsedTicks;

            await send().ConfigureAwait(false);

            var sendCompletionTimestamp = _stopwatch.ElapsedTicks;

            _sentMessages.TryAdd(messageId, new SentMessage(sendTimestamp, sendCompletionTimestamp));

            if (postSend)
                return;

            var sent = Interlocked.Increment(ref _sent);
            if (sent == _messageCount)
                _sendCompleted.TrySetResult(_stopwatch.Elapsed);
        }

        public async Task PostSend(Guid messageId)
        {
            var sendCompletionTimestamp = _stopwatch.ElapsedTicks;

            _sentMessages.AddOrUpdate(messageId, _ => new SentMessage().UpdateCompletion(sendCompletionTimestamp),
                (_, existing) => new SentMessage(existing.SendTimestamp, sendCompletionTimestamp));

            var sent = Interlocked.Increment(ref _sent);
            if (sent == _messageCount)
                _sendCompleted.TrySetResult(_stopwatch.Elapsed);
        }

        public MessageMetric[] GetMessageMetrics()
        {
            return _sentMessages.Join(_consumedMessages, x => x.Key, x => x.MessageId, (sent, consumed) =>
                    new MessageMetric(sent.Key, sent.Value.SendCompletionTimestamp - sent.Value.SendTimestamp,
                        consumed.Timestamp - sent.Value.SendTimestamp))
                .ToArray();
        }


        struct SentMessage
        {
            public readonly long SendTimestamp;
            public long SendCompletionTimestamp;

            public SentMessage(long sendTimestamp, long sendCompletionTimestamp)
            {
                SendTimestamp = sendTimestamp;
                SendCompletionTimestamp = sendCompletionTimestamp;
            }

            public SentMessage UpdateCompletion(long sendCompletionTimestamp)
            {
                SendCompletionTimestamp = Math.Max(sendCompletionTimestamp, SendCompletionTimestamp);

                return this;
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
