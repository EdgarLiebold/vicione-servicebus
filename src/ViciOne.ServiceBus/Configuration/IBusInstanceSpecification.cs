using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public interface IBusInstanceSpecification :
    ISpecification
{
    void Configure(IBusInstance busInstance);
}
