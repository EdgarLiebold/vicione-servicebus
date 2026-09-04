using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class BusTestPublishObserver :
    InactivityTestObserver,
    IPublishObserver
{
    readonly PublishedMessageList _messages;

    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
        : this(timeout, inactivityTimout, testCompleted, TimeProvider.System)
    {
    }

    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new PublishedMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimout);
    }

    public IPublishedMessageList Messages => _messages;

    Task IPublishObserver.PrePublishAsync<T>(PublishContext<T> context)
    {
        return RestartTimerAsync();
    }

    Task IPublishObserver.PostPublishAsync<T>(PublishContext<T> context)
    {
        _messages.Add(context);

        return RestartTimerAsync(false);
    }

    Task IPublishObserver.PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
    {
        _messages.Add(context, exception);

        return RestartTimerAsync(false);
    }
}
