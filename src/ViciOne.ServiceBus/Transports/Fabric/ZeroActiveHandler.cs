using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Represents the method that handles zero active handler.
/// </summary>
/// <returns>The result of the operation.</returns>
public delegate Task ZeroActiveHandler();
