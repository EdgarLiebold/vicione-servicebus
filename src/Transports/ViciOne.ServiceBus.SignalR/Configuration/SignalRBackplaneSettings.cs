using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.SignalR.Configuration;

/// <summary>Captures the immutable settings used by one registered hub backplane.</summary>
internal sealed record SignalRBackplaneSettings<THub>(RequestTimeout RemoteGroupOperationTimeout)
    where THub : Hub;
