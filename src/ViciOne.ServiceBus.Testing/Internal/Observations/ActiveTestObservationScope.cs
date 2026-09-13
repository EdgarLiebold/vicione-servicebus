using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

sealed class ActiveTestObservationScope :
    IConsumeObserver,
    IPublishObserver,
    ISendObserver,
    IDisposable
{
    readonly object _lock = new();
    readonly List<IConsumedMessage> _consumed = new();
    readonly HashSet<Guid> _consumedIds = new();
    readonly ConnectHandle _consumeHandle;
    readonly List<IPublishedMessage> _published = new();
    readonly HashSet<Guid> _publishedIds = new();
    readonly ConnectHandle _publishHandle;
    readonly List<ISentMessage> _sent = new();
    readonly HashSet<Guid> _sentIds = new();
    readonly ConnectHandle _sendHandle;
    readonly TimeProvider _timeProvider;
    readonly ActivityTraceId _traceId;
    int _disposed;

    public ActiveTestObservationScope(IBaseTestHarness harness, TimeProvider timeProvider, ActivityTraceId traceId)
    {
        ArgumentNullException.ThrowIfNull(harness);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _traceId = traceId;

        ConnectHandle? consumeHandle = null;
        ConnectHandle? publishHandle = null;
        ConnectHandle? sendHandle = null;
        try
        {
            consumeHandle = harness.ConnectConsumeObserver(this);
            publishHandle = harness.ConnectPublishObserver(this);
            sendHandle = harness.ConnectSendObserver(this);
        }
        catch (Exception startupException)
        {
            var failures = new List<Exception> { startupException };
            DisposeHandle(sendHandle, failures);
            DisposeHandle(publishHandle, failures);
            DisposeHandle(consumeHandle, failures);

            if (failures.Count == 1)
                ExceptionDispatchInfo.Capture(startupException).Throw();

            throw new AggregateException("Active test observation setup and cleanup both failed.", failures);
        }

        _consumeHandle = consumeHandle;
        _publishHandle = publishHandle;
        _sendHandle = sendHandle;
    }

    public ActiveTestResult Snapshot()
    {
        lock (_lock)
            return new ActiveTestResult(_consumed.ToArray(), _published.ToArray(), _sent.ToArray());
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        var failures = new List<Exception>();
        DisposeHandle(_sendHandle, failures);
        DisposeHandle(_publishHandle, failures);
        DisposeHandle(_consumeHandle, failures);

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("One or more active test observation connections could not be released.", failures);
    }

    public Task PreConsumeAsync<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
        => Task.CompletedTask;

    public Task PostConsumeAsync<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        AddConsumed(new ConsumedMessage<TMessage>(context, null, _timeProvider));
        return Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<TMessage>(ConsumeContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        AddConsumed(new ConsumedMessage<TMessage>(context, exception, _timeProvider));
        return Task.CompletedTask;
    }

    public Task PrePublishAsync<TMessage>(PublishContext<TMessage> context)
        where TMessage : class
        => Task.CompletedTask;

    public Task PostPublishAsync<TMessage>(PublishContext<TMessage> context)
        where TMessage : class
    {
        AddPublished(new PublishedMessage<TMessage>(context, null, _timeProvider));
        return Task.CompletedTask;
    }

    public Task PublishFaultAsync<TMessage>(PublishContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        AddPublished(new PublishedMessage<TMessage>(context, exception, _timeProvider));
        return Task.CompletedTask;
    }

    public Task PreSendAsync<TMessage>(SendContext<TMessage> context)
        where TMessage : class
        => Task.CompletedTask;

    public Task PostSendAsync<TMessage>(SendContext<TMessage> context)
        where TMessage : class
    {
        AddSent(new SentMessage<TMessage>(context, null, _timeProvider));
        return Task.CompletedTask;
    }

    public Task SendFaultAsync<TMessage>(SendContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        AddSent(new SentMessage<TMessage>(context, exception, _timeProvider));
        return Task.CompletedTask;
    }

    void AddConsumed(IConsumedMessage message)
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
        return Activity.Current?.TraceId == _traceId;
    }

    static void DisposeHandle(ConnectHandle? handle, ICollection<Exception> failures)
    {
        if (handle == null)
            return;

        try
        {
            handle.Dispose();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }

}
