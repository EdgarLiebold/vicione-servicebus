namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Generates a hash of the input data for partitioning purposes
/// </summary>
public interface IHashGenerator
{
    /// <summary>
    /// Determines whether the current value has h.
    /// </summary>
    /// <param name="data">The data value.</param>
    /// <returns>The result of the operation.</returns>
    uint Hash(byte[] data);
}
