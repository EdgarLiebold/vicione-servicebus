namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Defines the contract for symmetric key.
/// </summary>
public interface SymmetricKey
{
    /// <summary>
    /// Gets the key value.
    /// </summary>
    byte[] Key { get; }

    /// <summary>
    /// Gets the iv value.
    /// </summary>
    byte[] IV { get; }
}
