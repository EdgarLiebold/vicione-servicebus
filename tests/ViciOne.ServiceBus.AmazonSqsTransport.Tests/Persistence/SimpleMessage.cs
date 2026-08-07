// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests.Persistence
{
    public class SimpleMessage
    {
        public MessageData<string>? BigData { get; set; }
    }
}
