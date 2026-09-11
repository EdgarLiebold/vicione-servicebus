using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Serialization;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Provides inline text message data.</summary>
internal sealed class StringInlineMessageData :
    MessageData<string>,
    IInlineMessageData
{
    readonly string _value;

    /// <summary>Creates inline text with an optional repository address.</summary>
    /// <param name="value">The non-null text value.</param>
    /// <param name="address">The repository address, when the same text was also stored.</param>
    public StringInlineMessageData(string value, Uri? address = null)
    {
        Address = address;
        _value = value ?? throw new ArgumentNullException(nameof(value));

        Value = Task.FromResult<string?>(_value);
    }

    /// <inheritdoc />
    public void Set(IMessageDataReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        reference.Text = _value;
        reference.Data = default;
    }

    /// <inheritdoc />
    public Uri? Address { get; }

    /// <inheritdoc />
    public bool HasValue => true;

    /// <inheritdoc />
    public Task<string?> Value { get; }
}
