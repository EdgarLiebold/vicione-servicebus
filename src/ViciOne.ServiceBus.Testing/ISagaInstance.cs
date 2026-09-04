namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for saga instance.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ISagaInstance<out T> :
    IAsyncListElement
    where T : class, ISaga
{
    /// <summary>
    /// Gets the saga value.
    /// </summary>
    T Saga { get; }
}
