// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests.Persistence
{
    using System.Threading.Tasks;


    public class EmptyConsumer : IConsumer<SimpleMessage>
    {
        public Task Consume(ConsumeContext<SimpleMessage> context)
        {
            return Task.CompletedTask;
        }
    }
}
