namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by saga instance.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ISagaInstance<out T> :
    IAsyncListElement
    where T : class, ISaga
{
    /// <summary>Gets the saga.</summary>
    T Saga { get; }
}
