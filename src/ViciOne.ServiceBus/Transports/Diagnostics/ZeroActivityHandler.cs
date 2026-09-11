using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Handles the transition to zero active message dispatches.</summary>
/// <returns>A task that completes when the transition has been handled.</returns>
public delegate Task ZeroActivityHandler();
