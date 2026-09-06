namespace ViciOne.ServiceBus.Logging;

/// <summary>Represents the canonical name for log category.</summary>
public static class LogCategoryName
{
    /// <summary>Exposes the vici one service bus used by the containing type.</summary>
    public const string ViciOneServiceBus = "ViciOne.ServiceBus";


    /// <summary>Defines logging category names for transport components.</summary>
    public static class Transport
    {
        /// <summary>Exposes the receive used by the containing type.</summary>
        public const string Receive = "ViciOne.ServiceBus.ReceiveTransport";
        /// <summary>Exposes the send used by the containing type.</summary>
        public const string Send = "ViciOne.ServiceBus.SendTransport";
    }
}
