using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureStorage.MessageData;

/// <summary>
/// Provides a new id blob name generator implementation.
/// </summary>
public class NewIdBlobNameGenerator :
    IBlobNameGenerator
{
    /// <summary>
    /// Performs the generate blob name operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GenerateBlobName()
    {
        return FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());
    }
}
