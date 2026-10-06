namespace ViciOne.ServiceBus.Serialization;

/// <summary>Selects the active encryption key and resolves historical keys during key rotation.</summary>
public interface IEncryptionKeyProvider
{
    /// <summary>Gets the key used to encrypt new message-data objects.</summary>
    /// <returns>The active encryption key.</returns>
    EncryptionKey GetCurrentKey();

    /// <summary>Attempts to resolve the key named by an encrypted message-data envelope.</summary>
    /// <param name="keyId">The untrusted key selector decoded from the envelope before authentication.</param>
    /// <param name="key">The matching key when found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the key was found; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// Authentication requires the selected key and occurs after this lookup. An attacker can supply the
    /// selector; its presence does not establish sender identity, authorization, or envelope authenticity.
    /// Implement bounded lookup over keys controlled by this provider. Do not grant access or perform
    /// unbounded external resolution based on the selector. Plaintext is returned only after authentication succeeds.
    /// </remarks>
    bool TryGetKey(string keyId, out EncryptionKey? key);
}
