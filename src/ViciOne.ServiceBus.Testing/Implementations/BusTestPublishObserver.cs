using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a bus test publish observer implementation.
/// </summary>
public class BusTestPublishObserver :
    InactivityTestObserver,
    IPublishObserver
{
    readonly PublishedMessageList _messages;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="inactivityTimout">The inactivity timout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
        : this(timeout, inactivityTimout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="inactivityTimout">The inactivity timout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new PublishedMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimout);
    }

    /// <summary>
    /// Gets the messages value.
    /// </summary>
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
