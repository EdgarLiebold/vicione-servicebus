// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IInMemoryConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IMessageFabricConsumeTopologyBuilder builder);
    }
}
