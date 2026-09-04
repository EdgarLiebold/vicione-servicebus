using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a bus test consume observer implementation.
/// </summary>
public class BusTestConsumeObserver :
    InactivityTestObserver,
    IConsumeObserver
{
    readonly ReceivedMessageList _messages;
    int _activeCount;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted)
        : this(timeout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new ReceivedMessageList(timeout, testCompleted, timeProvider);
    }

    /// <summary>
    /// Gets the messages value.
    /// </summary>
    public IReceivedMessageList Messages => _messages;

    /// <summary>
    /// Gets the is inactive value.
    /// </summary>
    public override bool IsInactive => _activeCount == 0;

    /// <summary>
    /// Performs the pre consume operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        Interlocked.Increment(ref _activeCount);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post consume operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        _messages.Add(context);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }

    /// <summary>
    /// Consumes fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        _messages.Add(context, exception);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }
}
