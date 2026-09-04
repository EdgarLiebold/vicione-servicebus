using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// Provides an empty message data implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class EmptyMessageData<T> :
    MessageData<T>
{
    /// <summary>
    /// Defines the instance value.
    /// </summary>
    public static readonly MessageData<T> Instance = new EmptyMessageData<T>();

    EmptyMessageData()
    {
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri Address => throw new MessageDataException("The message data is empty");

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => false;

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<T?> Value => NoValueAsync();

    static Task<T?> NoValueAsync()
    {
        throw new MessageDataException("The message data is empty");
    }
}
