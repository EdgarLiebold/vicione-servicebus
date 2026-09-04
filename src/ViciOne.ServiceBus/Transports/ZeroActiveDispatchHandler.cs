using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Represents the method that handles zero active dispatch handler.
/// </summary>
/// <returns>The result of the operation.</returns>
public delegate Task ZeroActiveDispatchHandler();
