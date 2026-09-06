using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

sealed class ActiveTestObservationScope :
    IConsumeObserver,
    IPublishObserver,
    ISendObserver,
    IDisposable
{
    readonly object _lock = new();
    readonly List<IReceivedMessage> _consumed = new();
    readonly HashSet<Guid> _consumedIds = new();
    readonly ConnectHandle _consumeHandle;
    readonly List<IPublishedMessage> _published = new();
    readonly HashSet<Guid> _publishedIds = new();
    readonly ConnectHandle _publishHandle;
    readonly List<ISentMessage> _sent = new();
    readonly HashSet<Guid> _sentIds = new();
    readonly ConnectHandle _sendHandle;
    readonly TimeProvider _timeProvider;
    readonly ActivityTraceId? _traceId;
    int _disposed;

    public ActiveTestObservationScope(IBaseTestHarness harness, TimeProvider timeProvider, ActivityTraceId? traceId)
    {
        ArgumentNullException.ThrowIfNull(harness);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _traceId = traceId;

        _consumeHandle = harness.ConnectConsumeObserver(this);
        _publishHandle = harness.ConnectPublishObserver(this);
        _sendHandle = harness.ConnectSendObserver(this);
    }

    public ActiveTestResult Snapshot()
    {
        lock (_lock)
            return new ActiveTestResult(_consumed.ToArray(), _published.ToArray(), _sent.ToArray());
    }

    public void Dispose()
    {
        if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _sendHandle.Dispose();
        _publishHandle.Dispose();
        _consumeHandle.Dispose();
    }

    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
        => Task.CompletedTask;

    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        AddConsumed(new ReceivedMessage<T>(context, null, _timeProvider));
        return Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        AddConsumed(new ReceivedMessage<T>(context, exception, _timeProvider));
        return Task.CompletedTask;
    }

    public Task PrePublishAsync<T>(PublishContext<T> context)
        where T : class
        => Task.CompletedTask;

    public Task PostPublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        AddPublished(new PublishedMessage<T>(context, null, _timeProvider));
        return Task.CompletedTask;
    }

    public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        AddPublished(new PublishedMessage<T>(context, exception, _timeProvider));
        return Task.CompletedTask;
    }

    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
        => Task.CompletedTask;

    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        AddSent(new SentMessage<T>(context, null, _timeProvider));
        return Task.CompletedTask;
    }

    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        AddSent(new SentMessage<T>(context, exception, _timeProvider));
        return Task.CompletedTask;
    }

    void AddConsumed(IReceivedMessage message)
    {
        if (!IsTrackedTrace())
            return;

        lock (_lock)
        {
            if (message.ElementId is Guid id && !_consumedIds.Add(id))
                return;

            _consumed.Add(message);
        }
    }

    void AddPublished(IPublishedMessage message)
    {
        if (!IsTrackedTrace())
            return;

        lock (_lock)
        {
            if (message.ElementId is Guid id && !_publishedIds.Add(id))
                return;

            _published.Add(message);
        }
    }

    void AddSent(ISentMessage message)
    {
        if (!IsTrackedTrace())
            return;

        lock (_lock)
        {
            if (message.ElementId is Guid id && !_sentIds.Add(id))
                return;

            _sent.Add(message);
        }
    }

    bool IsTrackedTrace()
    {
        if (!_traceId.HasValue)
            return true;

        return Activity.Current?.TraceId == _traceId.Value;
    }

}
