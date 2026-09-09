namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures routing-slip changes applied after activity execution faults.</summary>
/// <param name="options">The fault options to configure.</param>
public delegate void ConfigureFaultedActivityOptionsCallback(FaultedActivityOptions options);
