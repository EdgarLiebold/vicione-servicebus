using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for handler test harness.
/// </summary>
public static class HandlerTestHarnessExtensions
{
    /// <summary>
    /// Performs the handler operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="handler">The handler value.</param>
    /// <returns>The result of the operation.</returns>
    public static HandlerTestHarness<T> Handler<T>(this BusTestHarness harness, MessageHandler<T> handler)
        where T : class
    {
        return new HandlerTestHarness<T>(harness, handler);
    }

    /// <summary>
    /// Performs the handler operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <returns>The result of the operation.</returns>
    public static HandlerTestHarness<T> Handler<T>(this BusTestHarness harness)
        where T : class
    {
        return new HandlerTestHarness<T>(harness, context => Task.CompletedTask);
    }
}
