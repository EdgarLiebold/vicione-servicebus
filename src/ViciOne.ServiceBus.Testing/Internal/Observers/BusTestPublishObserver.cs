using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Records publish attempts and restarts inactivity detection around each attempt.</summary>
internal sealed class BusTestPublishObserver :
    InactivityTestObserver,
    IPublishObserver
{
    readonly PublishedMessageList _messages;

    /// <summary>Creates a publish observer that uses the system clock.</summary>
    /// <param name="timeout">The maximum time an assertion waits for a matching publication.</param>
    /// <param name="inactivityTimeout">The interval without publish activity that indicates inactivity.</param>
    /// <param name="testCompleted">The token that ends pending test observations.</param>
    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimeout, CancellationToken testCompleted = default)
        : this(timeout, inactivityTimeout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>Creates a publish observer.</summary>
    /// <param name="timeout">The maximum time an assertion waits for a matching publication.</param>
    /// <param name="inactivityTimeout">The interval without publish activity that indicates inactivity.</param>
    /// <param name="testCompleted">The token that ends pending test observations.</param>
    /// <param name="timeProvider">The clock used for assertion timeouts, inactivity, and timestamps.</param>
    public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new PublishedMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimeout);
    }

    /// <summary>Gets the recorded publish attempts.</summary>
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
