// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests.Scenarios
{
    using System.Threading.Tasks;
    using TestFramework.Messages;


    public class PingRequestConsumer :
        IConsumer<PingMessage>
    {
        public Task Consume(ConsumeContext<PingMessage> context)
        {
            return context.RespondAsync(new PongMessage(context.Message.CorrelationId));
        }
    }
}
