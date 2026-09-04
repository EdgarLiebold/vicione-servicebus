using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs.Activities;

static class ProducerFactoryExtensions
{
    internal static Task<IEventHubProducer> GetProducerAsync<T>(this BehaviorContext<T> context, ConsumeContext consumeContext, string eventHubName)
        where T : class, SagaStateMachineInstance
    {
        return context.GetServiceOrCreateInstance<IEventHubRider>()
            .GetProducerProvider(consumeContext)
            .GetProducerAsync(eventHubName);
    }
}
