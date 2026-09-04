namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for new id formatter.
/// </summary>
public interface INewIdFormatter
{
    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="bytes">The bytes value.</param>
    /// <returns>The result of the operation.</returns>
    string Format(in byte[] bytes);
}
