namespace ViciOne.ServiceBus.Middleware;

/// <summary>Generates a hash of the input data for partitioning purposes.</summary>
public interface IHashGenerator
{
    /// <summary>Computes a hash code for the supplied value.</summary>
    /// <param name="data">The data.</param>
    /// <returns>The uint produced by the operation.</returns>
    uint Hash(byte[] data);
}
