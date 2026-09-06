using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes bus test send events.</summary>
public class BusTestSendObserver :
    InactivityTestObserver,
    ISendObserver
{
    readonly SentMessageList _messages;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    /// <param name="testCompleted">The test completed.</param>
    public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
        : this(timeout, inactivityTimout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new SentMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimout);
    }

    /// <summary>Gets the messages.</summary>
    public ISentMessageList Messages => _messages;

    /// <summary>Runs before send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return RestartTimerAsync();
    }

    /// <summary>Runs after send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        _messages.Add(context);

        return RestartTimerAsync(false);
    }

    /// <summary>Sends fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        _messages.Add(context, exception);

        return RestartTimerAsync(false);
    }
}
