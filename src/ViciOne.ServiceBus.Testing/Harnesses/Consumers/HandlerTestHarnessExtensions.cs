using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates message-handler observers for a bus test harness.</summary>
public static class HandlerTestHarnessExtensions
{
    /// <summary>Registers a message handler and records its deliveries.</summary>
    /// <typeparam name="TMessage">The handled message contract.</typeparam>
    /// <param name="harness">The harness that hosts the handler.</param>
    /// <param name="handler">The handler invoked for each matching message.</param>
    /// <returns>A harness that records successful and faulted deliveries.</returns>
    public static HandlerTestHarness<TMessage> AddHandler<TMessage>(this BusTestHarness harness, MessageHandler<TMessage> handler)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(handler);
        return new HandlerTestHarness<TMessage>(harness, handler);
    }

    /// <summary>Registers a successful no-op handler and records its deliveries.</summary>
    /// <typeparam name="TMessage">The handled message contract.</typeparam>
    /// <param name="harness">The harness that hosts the handler.</param>
    /// <returns>A harness that records matching deliveries.</returns>
    public static HandlerTestHarness<TMessage> AddHandler<TMessage>(this BusTestHarness harness)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        return new HandlerTestHarness<TMessage>(harness, static _ => Task.CompletedTask);
    }
}
