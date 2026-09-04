namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Defines the contract for secure key provider.
/// </summary>
public interface ISecureKeyProvider :
    IProbeSite
{
    /// <summary>
    /// Gets key.
    /// </summary>
    /// <param name="headers">The headers value.</param>
    /// <returns>The result of the operation.</returns>
    byte[] GetKey(Headers headers);
}
