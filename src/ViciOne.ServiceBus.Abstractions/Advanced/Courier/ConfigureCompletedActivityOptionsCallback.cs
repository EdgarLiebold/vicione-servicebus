namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures routing-slip changes applied after successful activity execution.</summary>
/// <param name="options">The completion options to configure.</param>
public delegate void ConfigureCompletedActivityOptionsCallback(CompletedActivityOptions options);
