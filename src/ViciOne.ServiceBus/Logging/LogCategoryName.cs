// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Logging
{
    public static class LogCategoryName
    {
        public const string ViciOneServiceBus = "ViciOne.ServiceBus";


        public static class Transport
        {
            public const string Receive = "ViciOne.ServiceBus.ReceiveTransport";
            public const string Send = "ViciOne.ServiceBus.SendTransport";
        }
    }
}
