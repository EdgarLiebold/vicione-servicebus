using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Carries empty message data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class EmptyMessageData<T> :
    MessageData<T>
{
    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly MessageData<T> Instance = new EmptyMessageData<T>();

    EmptyMessageData()
    {
    }

    /// <summary>Gets the address.</summary>
    public Uri Address => throw new MessageDataException("The message data is empty");

    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue => false;

    /// <summary>Gets the value.</summary>
    public Task<T?> Value => NoValueAsync();

    static Task<T?> NoValueAsync()
    {
        throw new MessageDataException("The message data is empty");
    }
}
