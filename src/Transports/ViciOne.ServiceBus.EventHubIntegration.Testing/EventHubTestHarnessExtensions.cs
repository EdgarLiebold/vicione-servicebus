using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus;

public static class EventHubTestHarnessExtensions
{
    public static Task<IEventHubProducer> GetProducerAsync(this ITestHarness harness, string eventHubName, CancellationToken cancellationToken = default)
    {
        return harness.Scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>().GetProducerAsync(eventHubName, cancellationToken: cancellationToken);
    }
}
