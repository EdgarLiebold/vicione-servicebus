using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Carries inline message data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class InlineMessageData<T> :
    MessageData<T>,
    IInlineMessageData
{
    readonly IInlineMessageData _messageData;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="messageData">The message data.</param>
    public InlineMessageData(Uri? address, T value, IInlineMessageData messageData)
    {
        Value = Task.FromResult<T?>(value);
        Address = address;

        _messageData = messageData;
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="reference">The reference.</param>
    public void Set(IMessageDataReference reference)
    {
        _messageData.Set(reference);
    }

    /// <summary>Gets the address.</summary>
    public Uri? Address { get; }
    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue => true;
    /// <summary>Gets the value.</summary>
    public Task<T?> Value { get; }
}
