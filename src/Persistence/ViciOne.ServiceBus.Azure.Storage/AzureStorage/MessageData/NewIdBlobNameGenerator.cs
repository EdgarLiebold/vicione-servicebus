// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureStorage.MessageData
{
    using Util;


    public class NewIdBlobNameGenerator :
        IBlobNameGenerator
    {
        public string GenerateBlobName()
        {
            return FormatUtil.Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray());
        }
    }
}
