using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the transport-independent bus factory selector.</summary>
public static class Bus
{
    /// <summary>Gets the selector used to choose a bus factory.</summary>
    public static IBusFactorySelector Factory { get; } = new BusFactorySelector();
}
