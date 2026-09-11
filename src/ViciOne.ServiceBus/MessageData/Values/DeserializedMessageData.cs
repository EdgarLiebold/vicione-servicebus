using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Represents an external reference that must be replaced by the consume transform before its value is read.</summary>
/// <typeparam name="T">The value type addressed by the reference.</typeparam>
internal sealed class DeserializedMessageData<T> :
    MessageData<T>
{
    /// <summary>Creates an unresolved reference for a repository address.</summary>
    /// <param name="address">The non-null repository address.</param>
    public DeserializedMessageData(Uri address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <inheritdoc />
    public Uri Address { get; }

    /// <inheritdoc />
    public bool HasValue => true;

    /// <summary>Throws because repository-backed values are available only after consume transformation.</summary>
    public Task<T?> Value => throw new MessageDataException("The message data was not loaded: " + Address);
}
