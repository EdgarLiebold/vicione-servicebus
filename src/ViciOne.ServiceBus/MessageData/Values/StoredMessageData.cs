using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// MessageData that has been stored by the repository, has a valid address, and is ready to
/// be serialized.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public class StoredMessageData<T> :
    MessageData<T>
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    /// <param name="value">The value to process.</param>
    public StoredMessageData(Uri? address, T value)
    {
        Address = address;
        Value = Task.FromResult<T?>(value);
    }

    /// <summary>Gets the address.</summary>
    public Uri? Address { get; }

    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue => true;

    /// <summary>Gets the value.</summary>
    public Task<T?> Value { get; }
}
