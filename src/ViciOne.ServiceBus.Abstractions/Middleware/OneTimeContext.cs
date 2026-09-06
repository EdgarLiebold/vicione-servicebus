namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Exposes state for one time operations.</summary>
/// <typeparam name="TPayload">The payload type.</typeparam>
public interface OneTimeContext<TPayload>
    where TPayload : class
{
    /// <summary>Evicts the selected cached entry.</summary>
    void Evict();
}
