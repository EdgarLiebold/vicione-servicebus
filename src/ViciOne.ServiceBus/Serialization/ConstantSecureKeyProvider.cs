namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a constant secure key provider implementation.
/// </summary>
public class ConstantSecureKeyProvider :
    ISecureKeyProvider
{
    readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="key">The key value.</param>
    public ConstantSecureKeyProvider(byte[] key)
    {
        _key = key;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("key", "constant");
    }

    /// <summary>
    /// Gets key.
    /// </summary>
    /// <param name="headers">The headers value.</param>
    /// <returns>The result of the operation.</returns>
    public byte[] GetKey(Headers headers)
    {
        return _key;
    }
}
