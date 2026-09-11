namespace ViciOne.ServiceBus.Serialization;

/// <summary>Selects the active encryption key and resolves historical keys during key rotation.</summary>
public interface IEncryptionKeyProvider
{
    /// <summary>Gets the key used to encrypt new message-data objects.</summary>
    /// <returns>The active encryption key.</returns>
    EncryptionKey GetCurrentKey();

    /// <summary>Attempts to resolve the key named by an encrypted message-data envelope.</summary>
    /// <param name="keyId">The key identifier read from the authenticated envelope.</param>
    /// <param name="key">The matching key when found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the key was found; otherwise, <see langword="false" />.</returns>
    bool TryGetKey(string keyId, out EncryptionKey? key);
}
