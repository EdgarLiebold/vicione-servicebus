// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Configuration;


    /// <summary>
    /// used to get access to the bus factories
    /// </summary>
    public static class Bus
    {
        /// <summary>
        /// Access a bus factory from this main factory interface (easy extension method support)
        /// </summary>
        public static IBusFactorySelector Factory { get; } = new BusFactorySelector();
    }
}
