using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes bus test publish events.</summary>
public class BusTestPublishObserver :
    InactivityTestObserver,
    IPublishObserver
{
    readonly PublishedMessageList _messages;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    /// <param name="testCompleted">The test completed.</param>
    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
        : this(timeout, inactivityTimout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new PublishedMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimout);
    }

    /// <summary>Gets the messages.</summary>
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
