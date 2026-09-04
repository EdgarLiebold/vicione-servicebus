namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Defines the contract for one time context.
/// </summary>
/// <typeparam name="TPayload">The t payload type.</typeparam>
public interface OneTimeContext<TPayload>
    where TPayload : class
{
    /// <summary>
    /// Performs the evict operation.
    /// </summary>
    void Evict();
}
