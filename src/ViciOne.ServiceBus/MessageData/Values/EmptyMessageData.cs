using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Provides the shared handle used when a message-data property has no value or address.</summary>
/// <typeparam name="T">The optional value type.</typeparam>
internal sealed class EmptyMessageData<T> :
    MessageData<T>
{
    /// <summary>Gets the shared empty value for this closed generic type.</summary>
    internal static MessageData<T> Instance { get; } = new EmptyMessageData<T>();

    EmptyMessageData()
    {
    }

    /// <summary>Throws because an empty value has no repository address.</summary>
    public Uri Address => throw new MessageDataException("The message data is empty");

    /// <inheritdoc />
    public bool HasValue => false;

    /// <summary>Throws because no value is present.</summary>
    public Task<T?> Value => NoValueAsync();

    static Task<T?> NoValueAsync()
    {
        throw new MessageDataException("The message data is empty");
    }
}
