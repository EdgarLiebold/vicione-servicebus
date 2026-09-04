using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// Message data that needs to be stored in the repository when the message is sent.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PutMessageData<T> :
    MessageData<T>
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="hasValue">The has value value.</param>
    public PutMessageData(T value, bool hasValue = true)
    {
        HasValue = hasValue;
        Value = Task.FromResult<T?>(value);
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri? Address => null;
    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue { get; }
    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<T?> Value { get; }
}
