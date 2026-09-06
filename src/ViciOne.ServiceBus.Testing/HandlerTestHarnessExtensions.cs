using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for handler test harness.</summary>
public static class HandlerTestHarnessExtensions
{
    /// <summary>Creates a test harness for the selected message handler.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The handler test harness produced by the operation.</returns>
    public static HandlerTestHarness<T> Handler<T>(this BusTestHarness harness, MessageHandler<T> handler)
        where T : class
    {
        return new HandlerTestHarness<T>(harness, handler);
    }

    /// <summary>Creates a test harness for the selected message handler.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <returns>The handler test harness produced by the operation.</returns>
    public static HandlerTestHarness<T> Handler<T>(this BusTestHarness harness)
        where T : class
    {
        return new HandlerTestHarness<T>(harness, context => Task.CompletedTask);
    }
}
