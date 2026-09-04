using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.EventHubs.Testing;

/// <summary>
/// Provides extension methods for event hub test harness.
/// </summary>
public static class EventHubTestHarnessExtensions
{
    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="harness">The harness value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<IEventHubProducer> GetProducerAsync(this ITestHarness harness, string eventHubName, CancellationToken cancellationToken = default)
    {
        return harness.Scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>().GetProducerAsync(eventHubName, cancellationToken: cancellationToken);
    }
}
