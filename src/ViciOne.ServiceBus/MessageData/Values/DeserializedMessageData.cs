using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// When a message data property is deserialized, this is used as a placeholder for the actual message
/// data accessor which replaces this property value once the message is transformed on the pipeline.
/// </summary>
/// <typeparam name="T">
/// The type used to access the message data, valid types include stream, string, and byte[].
/// </typeparam>
public class DeserializedMessageData<T> :
    MessageData<T>
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public DeserializedMessageData(Uri address)
    {
        Address = address;
        HasValue = true;
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri Address { get; }
    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<T?> Value
    {
        get
        {
            if (HasValue == false)
                throw new MessageDataException("The message data has no value");

            throw new MessageDataException("The message data was not loaded: " + Address);
        }
    }
}
