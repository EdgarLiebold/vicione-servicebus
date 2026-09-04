using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus;

public static class EventHubTestHarnessExtensions
{
    public static Task<IEventHubProducer> GetProducer(this ITestHarness harness, string eventHubName)
    {
        return harness.Scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>().GetProducer(eventHubName);
    }
}
