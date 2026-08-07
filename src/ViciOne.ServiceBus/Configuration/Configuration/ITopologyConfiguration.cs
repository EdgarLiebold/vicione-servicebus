// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface ITopologyConfiguration :
        ISpecification
    {
        IMessageTopologyConfigurator Message { get; }
        ISendTopologyConfigurator Send { get; }
        IPublishTopologyConfigurator Publish { get; }
        IConsumeTopologyConfigurator Consume { get; }
    }
}
