using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Represents the method that handles zero active dispatch handler.</summary>
/// <returns>The value produced by the operation.</returns>
public delegate Task ZeroActiveDispatchHandler();
