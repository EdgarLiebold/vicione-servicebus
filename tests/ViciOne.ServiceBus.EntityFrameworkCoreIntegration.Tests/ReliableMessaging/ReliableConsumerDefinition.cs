// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.ReliableMessaging
{
    using ViciOne.ServiceBus.Tests.ReliableMessaging;


    public class ReliableConsumerDefinition :
        ConsumerDefinition<ReliableConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ReliableConsumer> consumerConfigurator, IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(r => r.Intervals(10, 50, 100, 100, 100, 100, 100, 100));

            endpointConfigurator.UseEntityFrameworkOutbox<ReliableDbContext>(context);
        }
    }
}
