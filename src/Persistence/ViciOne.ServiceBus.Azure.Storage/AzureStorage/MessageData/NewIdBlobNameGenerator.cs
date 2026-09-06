using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureStorage.MessageData;

/// <summary>Generates compact blob names from sequential <see cref="NewId"/> values.</summary>
public class NewIdBlobNameGenerator :
    IBlobNameGenerator
{
    /// <summary>Generates the next sequential message-data blob name.</summary>
    /// <returns>The formatted bytes of a newly generated sequential identifier.</returns>
    public string GenerateBlobName()
    {
        return FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());
    }
}
