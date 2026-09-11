using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Serialization;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Combines a typed value with its serialized inline representation.</summary>
/// <typeparam name="T">The typed value exposed to the message consumer.</typeparam>
internal sealed class InlineMessageData<T> :
    MessageData<T>,
    IInlineMessageData
{
    readonly IInlineMessageData _messageData;

    /// <summary>Creates a typed wrapper over an inline serialization.</summary>
    /// <param name="address">The repository address, when the inline representation was also stored.</param>
    /// <param name="value">The non-null typed value.</param>
    /// <param name="messageData">The inline representation written during serialization.</param>
    public InlineMessageData(Uri? address, T value, IInlineMessageData messageData)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = Task.FromResult<T?>(value);
        Address = address;

        _messageData = messageData ?? throw new ArgumentNullException(nameof(messageData));
    }

    /// <inheritdoc />
    public void Set(IMessageDataReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        _messageData.Set(reference);
    }

    /// <inheritdoc />
    public Uri? Address { get; }
    /// <inheritdoc />
    public bool HasValue => true;
    /// <inheritdoc />
    public Task<T?> Value { get; }
}
