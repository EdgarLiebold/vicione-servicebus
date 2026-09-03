namespace ViciOne.ServiceBus.Configuration
{
    using Transports;


    public interface IBusInstanceSpecification :
        ISpecification
    {
        void Configure(IBusInstance busInstance);
    }
}
