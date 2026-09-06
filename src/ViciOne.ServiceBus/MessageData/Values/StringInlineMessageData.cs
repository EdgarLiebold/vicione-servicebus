using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Carries string inline message data.</summary>
public class StringInlineMessageData :
    MessageData<string>,
    IInlineMessageData
{
    readonly string _value;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="address">The address.</param>
    public StringInlineMessageData(string value, Uri? address = null)
    {
        Address = address;
        _value = value;

        Value = Task.FromResult<string?>(value);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="reference">The reference.</param>
    public void Set(IMessageDataReference reference)
    {
        reference.Text = _value;
        reference.Data = default;
    }

    /// <summary>Gets the address.</summary>
    public Uri? Address { get; }

    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue => true;

    /// <summary>Gets the value.</summary>
    public Task<string?> Value { get; }
}
