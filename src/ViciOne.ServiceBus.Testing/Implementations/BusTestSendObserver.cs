using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a bus test send observer implementation.
/// </summary>
public class BusTestSendObserver :
    InactivityTestObserver,
    ISendObserver
{
    readonly SentMessageList _messages;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="inactivityTimout">The inactivity timout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
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
    public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new SentMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimout);
    }

    /// <summary>
    /// Gets the messages value.
    /// </summary>
    public ISentMessageList Messages => _messages;

    /// <summary>
    /// Performs the pre send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return RestartTimerAsync();
    }

    /// <summary>
    /// Performs the post send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        _messages.Add(context);

        return RestartTimerAsync(false);
    }

    /// <summary>
    /// Sends fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        _messages.Add(context, exception);

        return RestartTimerAsync(false);
    }
}
