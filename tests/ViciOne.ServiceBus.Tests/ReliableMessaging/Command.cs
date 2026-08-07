// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ReliableMessaging
{
    public class Command
    {
        public bool FailWhenConsuming { get; set; }
        public int? FailAfterProducing { get; set; }
    }
}
