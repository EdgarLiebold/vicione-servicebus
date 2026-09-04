namespace ViciOne.ServiceBus.AzureStorage.MessageData;

/// <summary>
/// Defines the contract for blob name generator.
/// </summary>
public interface IBlobNameGenerator
{
    /// <summary>
    /// Performs the generate blob name operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    string GenerateBlobName();
}
