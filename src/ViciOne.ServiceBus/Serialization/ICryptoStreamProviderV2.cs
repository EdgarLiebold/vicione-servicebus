using System.IO;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Defines the contract for crypto stream provider v2.
/// </summary>
public interface ICryptoStreamProviderV2 :
    IProbeSite
{
    /// <summary>
    /// Gets decrypt stream.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="headers">The headers value.</param>
    /// <returns>The result of the operation.</returns>
    Stream GetDecryptStream(Stream stream, Headers headers);

    /// <summary>
    /// Gets encrypt stream.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="headers">The headers value.</param>
    /// <returns>The result of the operation.</returns>
    Stream GetEncryptStream(Stream stream, Headers headers);
}
