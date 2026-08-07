// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.MessageData
{
    using NewIdFormatters;


    public class NewIdFileNameGenerator :
        IFileNameGenerator
    {
        public string GenerateFileName()
        {
            return ZBase32Formatter.LowerCase.Format(NewId.Next().ToSequentialGuid().ToByteArray());
        }
    }
}
