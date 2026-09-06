using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Represents the method that handles zero active handler.</summary>
/// <returns>The value produced by the operation.</returns>
public delegate Task ZeroActiveHandler();
