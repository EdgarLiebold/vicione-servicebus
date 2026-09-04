using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// MessageData that has been stored by the repository, has a valid address, and is ready to
/// be serialized.
/// </summary>
/// <typeparam name="T"></typeparam>
public class StoredMessageData<T> :
    MessageData<T>
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="value">The value.</param>
    public StoredMessageData(Uri? address, T value)
    {
        Address = address;
        Value = Task.FromResult<T?>(value);
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri? Address { get; }

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => true;

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<T?> Value { get; }
}
