namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IInMemoryConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IMessageFabricConsumeTopologyBuilder builder);
    }
}
