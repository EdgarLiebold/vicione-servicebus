namespace ViciOne.ServiceBus.Testing;

/// <summary>Represents a recorded saga instance.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaInstance<out TSaga> :
    IAsyncListElement
    where TSaga : class, ISaga
{
    /// <summary>Gets the recorded saga state.</summary>
    TSaga Saga { get; }
}
