#nullable enable
namespace ViciOne.ServiceBus.Logging;

public static class LogCategoryName
{
    public const string ViciOneServiceBus = "ViciOne.ServiceBus";


    public static class Transport
    {
        public const string Receive = "ViciOne.ServiceBus.ReceiveTransport";
        public const string Send = "ViciOne.ServiceBus.SendTransport";
    }
}
