using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Provides asynchronous access to an optional inline or claim-check-backed message value.</summary>
/// <typeparam name="T">The exposed value type.</typeparam>
public interface MessageData<T> :
    IMessageData
{
    /// <summary>Gets the value, loading repository-backed data when necessary.</summary>
    Task<T?> Value { get; }
}
