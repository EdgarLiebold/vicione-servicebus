using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.EventHubs.Testing;

/// <summary>Resolves Event Hubs producers from a running service-bus test harness.</summary>
public static class EventHubTestHarnessExtensions
{
    /// <summary>Gets a producer for the named Event Hub from the harness scope.</summary>
    /// <param name="harness">The running test harness.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the producer resolved from the harness service provider.</returns>
    public static Task<IEventHubProducer> GetProducerAsync(this ITestHarness harness, string eventHubName, CancellationToken cancellationToken = default)
    {
        return harness.Scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>().GetProducerAsync(eventHubName, cancellationToken: cancellationToken);
    }
}
