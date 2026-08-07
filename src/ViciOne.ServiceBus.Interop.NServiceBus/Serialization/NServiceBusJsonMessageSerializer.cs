// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    using System.Net.Mime;


    public class NServiceBusJsonMessageSerializer :
        IMessageSerializer
    {
        public const string ContentTypeHeaderValue = "application/json";
        public static readonly ContentType JsonContentType = new ContentType(ContentTypeHeaderValue);

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class
        {
            context.SetNServiceBusHeaders();

            return new NewtonsoftRawJsonMessageBody<T>(context);
        }

        public ContentType ContentType => JsonContentType;
    }
}
