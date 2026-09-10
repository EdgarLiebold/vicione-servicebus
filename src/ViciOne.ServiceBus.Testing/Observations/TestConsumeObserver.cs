using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Records successful and faulted consumptions observed through an <see cref="IConsumeObserver"/> connection.</summary>
public sealed class TestConsumeObserver :
    IConsumeObserver
{
    readonly ConsumedMessageList _messages;

    /// <summary>Creates an observer that uses the system clock.</summary>
    /// <param name="timeout">The maximum time a query waits for a matching consumption.</param>
    /// <param name="inactivityToken">The token canceled when the owning harness becomes inactive.</param>
    public TestConsumeObserver(TimeSpan timeout, CancellationToken inactivityToken)
        : this(timeout, inactivityToken, TimeProvider.System)
    {
    }

    /// <summary>Creates an observer.</summary>
    /// <param name="timeout">The maximum time a query waits for a matching consumption.</param>
    /// <param name="inactivityToken">The token canceled when the owning harness becomes inactive.</param>
    /// <param name="timeProvider">The clock used for query timeouts and observation timestamps.</param>
    public TestConsumeObserver(TimeSpan timeout, CancellationToken inactivityToken, TimeProvider timeProvider)
    {
        _messages = new ConsumedMessageList(timeout, inactivityToken, timeProvider);
    }

    /// <summary>Gets the recorded consumption attempts.</summary>
    public IConsumedMessageList Messages => _messages;

    Task IConsumeObserver.PreConsumeAsync<T>(ConsumeContext<T> context)
    {
        return Task.CompletedTask;
    }

    Task IConsumeObserver.PostConsumeAsync<T>(ConsumeContext<T> context)
    {
        _messages.Add(context);

        return Task.CompletedTask;
    }

    Task IConsumeObserver.ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
    {
        _messages.Add(context, exception);

        return Task.CompletedTask;
    }
}
