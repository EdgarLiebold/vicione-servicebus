using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a test consume observer implementation.
/// </summary>
public class TestConsumeObserver :
    IConsumeObserver
{
    readonly ReceivedMessageList _messages;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="inactivityToken">The inactivity token value.</param>
    public TestConsumeObserver(TimeSpan timeout, CancellationToken inactivityToken)
        : this(timeout, inactivityToken, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="inactivityToken">The inactivity token value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public TestConsumeObserver(TimeSpan timeout, CancellationToken inactivityToken, TimeProvider timeProvider)
    {
        _messages = new ReceivedMessageList(timeout, inactivityToken, timeProvider);
    }

    /// <summary>
    /// Gets the messages value.
    /// </summary>
    public IReceivedMessageList Messages => _messages;

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
