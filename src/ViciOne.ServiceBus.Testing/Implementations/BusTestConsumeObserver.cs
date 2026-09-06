using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes bus test consume events.</summary>
public class BusTestConsumeObserver :
    InactivityTestObserver,
    IConsumeObserver
{
    readonly ReceivedMessageList _messages;
    int _activeCount;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted)
        : this(timeout, testCompleted, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new ReceivedMessageList(timeout, testCompleted, timeProvider);
    }

    /// <summary>Gets the messages.</summary>
    public IReceivedMessageList Messages => _messages;

    /// <summary>Gets a value indicating whether inactive.</summary>
    public override bool IsInactive => _activeCount == 0;

    /// <summary>Runs before consume.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        Interlocked.Increment(ref _activeCount);

        return Task.CompletedTask;
    }

    /// <summary>Runs after consume.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        _messages.Add(context);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }

    /// <summary>Consumes fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        _messages.Add(context, exception);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }
}
