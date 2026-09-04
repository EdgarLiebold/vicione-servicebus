using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureStorage.MessageData;

public class NewIdBlobNameGenerator :
    IBlobNameGenerator
{
    public string GenerateBlobName()
    {
        return FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());
    }
}
