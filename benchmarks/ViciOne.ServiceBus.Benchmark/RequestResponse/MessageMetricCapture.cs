using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public class MessageMetricCapture :
    IReportConsumerMetric
{
    readonly TaskCompletionSource<TimeSpan> _consumeCompleted;
    readonly ConcurrentBag<ConsumedMessage> _consumedMessages;
    readonly long _messageCount;
    readonly TaskCompletionSource<TimeSpan> _requestCompleted;
    readonly ConcurrentBag<RequestResponseMessage> _sentMessages;
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
        _sentMessages = new ConcurrentBag<RequestResponseMessage>();
        _requestCompleted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
        _consumeCompleted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task<TimeSpan> RequestCompleted => _requestCompleted.Task;
    public Task<TimeSpan> ConsumeCompleted => _consumeCompleted.Task;

    Task IReportConsumerMetric.Consumed<T>(Guid messageId)
    {
        _consumedMessages.Add(new ConsumedMessage(messageId, _clock.ElapsedTicks));

        var consumed = Interlocked.Increment(ref _consumed);
        if (consumed == _messageCount)
            _consumeCompleted.TrySetResult(_clock.Elapsed);

        return TaskResults.Completed;
    }

    public async Task<T> ResponseReceived<T>(Guid messageId, Func<Task<T>> request)
        where T : class
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var sendTimestamp = _clock.ElapsedTicks;

        var response = await request().ConfigureAwait(false);

        var responseTimestamp = _clock.ElapsedTicks;

        _sentMessages.Add(new RequestResponseMessage(messageId, sendTimestamp, responseTimestamp));

        var sent = Interlocked.Increment(ref _sent);
        if (sent == _messageCount)
            _requestCompleted.TrySetResult(_clock.Elapsed);

        return response;
    }

    public MessageMetric[] GetMessageMetrics()
    {
        return _sentMessages.Join(_consumedMessages, x => x.MessageId, x => x.MessageId,
                (sent, consumed) =>
                    new MessageMetric(sent.MessageId, sent.ResponseTimestamp - sent.SendTimestamp,
                        consumed.Timestamp - sent.SendTimestamp))
            .ToArray();
    }


    struct RequestResponseMessage
    {
        public readonly Guid MessageId;
        public readonly long SendTimestamp;
        public readonly long ResponseTimestamp;

        public RequestResponseMessage(Guid messageId, long sendTimestamp, long responseTimestamp)
        {
            MessageId = messageId;
            SendTimestamp = sendTimestamp;
            ResponseTimestamp = responseTimestamp;
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
