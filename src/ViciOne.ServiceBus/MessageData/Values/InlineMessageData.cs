using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// Provides an inline message data implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class InlineMessageData<T> :
    MessageData<T>,
    IInlineMessageData
{
    readonly IInlineMessageData _messageData;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="value">The value.</param>
    /// <param name="messageData">The message data value.</param>
    public InlineMessageData(Uri? address, T value, IInlineMessageData messageData)
    {
        Value = Task.FromResult<T?>(value);
        Address = address;

        _messageData = messageData;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="reference">The reference value.</param>
    public void Set(IMessageDataReference reference)
    {
        _messageData.Set(reference);
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
