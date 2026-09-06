using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Message data that needs to be stored in the repository when the message is sent.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class PutMessageData<T> :
    MessageData<T>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="hasValue">The has value.</param>
    public PutMessageData(T value, bool hasValue = true)
    {
        HasValue = hasValue;
        Value = Task.FromResult<T?>(value);
    }

    /// <summary>Gets the address.</summary>
    public Uri? Address => null;
    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue { get; }
    /// <summary>Gets the value.</summary>
    public Task<T?> Value { get; }
}
