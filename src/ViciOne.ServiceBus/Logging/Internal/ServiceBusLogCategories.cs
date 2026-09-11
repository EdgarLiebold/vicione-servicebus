namespace ViciOne.ServiceBus.Logging.Internal;

/// <summary>Defines the stable logging categories owned by the core runtime.</summary>
internal static class ServiceBusLogCategories
{
    internal const string Root = "ViciOne.ServiceBus";
    internal const string Messages = "ViciOne.ServiceBus.Messages";
    internal const string ReceiveTransport = "ViciOne.ServiceBus.ReceiveTransport";
    internal const string SendTransport = "ViciOne.ServiceBus.SendTransport";
}
