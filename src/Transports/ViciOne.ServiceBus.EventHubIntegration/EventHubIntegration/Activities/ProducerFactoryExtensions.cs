// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Activities
{
    using System.Threading.Tasks;


    static class ProducerFactoryExtensions
    {
        internal static Task<IEventHubProducer> GetProducer<T>(this BehaviorContext<T> context, ConsumeContext consumeContext, string eventHubName)
            where T : class, SagaStateMachineInstance
        {
            return context.GetServiceOrCreateInstance<IEventHubRider>()
                .GetProducerProvider(consumeContext)
                .GetProducer(eventHubName);
        }
    }
}
