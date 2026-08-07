// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IInMemoryTopologyConfiguration :
        ITopologyConfiguration
    {
        new IInMemoryPublishTopologyConfigurator Publish { get; }

        new IInMemoryConsumeTopologyConfigurator Consume { get; }
    }
}
