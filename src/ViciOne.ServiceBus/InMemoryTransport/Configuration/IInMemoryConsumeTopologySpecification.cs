using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

public interface IInMemoryConsumeTopologySpecification :
    ISpecification
{
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
