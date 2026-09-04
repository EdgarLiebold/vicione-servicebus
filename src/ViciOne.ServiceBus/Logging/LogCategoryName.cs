#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides a log category name implementation.
/// </summary>
public static class LogCategoryName
{
    /// <summary>
    /// Defines the vici one service bus value.
    /// </summary>
    public const string ViciOneServiceBus = "ViciOne.ServiceBus";


    /// <summary>
    /// Provides a transport implementation.
    /// </summary>
    public static class Transport
    {
        /// <summary>
        /// Defines the receive value.
        /// </summary>
        public const string Receive = "ViciOne.ServiceBus.ReceiveTransport";
        /// <summary>
        /// Defines the send value.
        /// </summary>
        public const string Send = "ViciOne.ServiceBus.SendTransport";
    }
}
