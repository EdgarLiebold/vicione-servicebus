using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SignalR;

var services = new ServiceCollection();
services.AddViciOneServiceBus(configuration =>
{
    configuration.Limits(MessageLimits.Conservative);
    configuration.AddSignalRBackplane<PackageConsumerHub>(options =>
        options.RemoteGroupOperationTimeout = TimeSpan.FromSeconds(7));
});

if (!services.Any(descriptor => descriptor.ServiceType == typeof(HubLifetimeManager<PackageConsumerHub>)))
    throw new InvalidOperationException("The SignalR package did not register its bus-backed hub lifetime manager.");

if (typeof(SignalRBackplaneOptions).Assembly.GetName().Name != "ViciOne.ServiceBus.SignalR")
    throw new InvalidOperationException("The SignalR package assembly was not loaded.");

sealed class PackageConsumerHub : Hub;
