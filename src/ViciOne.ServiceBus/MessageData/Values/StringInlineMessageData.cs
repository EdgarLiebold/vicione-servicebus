using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// Provides a string inline message data implementation.
/// </summary>
public class StringInlineMessageData :
    MessageData<string>,
    IInlineMessageData
{
    readonly string _value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="address">The address value.</param>
    public StringInlineMessageData(string value, Uri? address = null)
    {
        Address = address;
        _value = value;

        Value = Task.FromResult<string?>(value);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="reference">The reference value.</param>
    public void Set(IMessageDataReference reference)
    {
        reference.Text = _value;
        reference.Data = default;
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
    public Task<string?> Value { get; }
}
