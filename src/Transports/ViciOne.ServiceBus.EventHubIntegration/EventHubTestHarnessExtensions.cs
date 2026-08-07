// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Testing;


    public static class EventHubTestHarnessExtensions
    {
        public static Task<IEventHubProducer> GetProducer(this ITestHarness harness, string eventHubName)
        {
            return harness.Scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>().GetProducer(eventHubName);
        }
    }
}
