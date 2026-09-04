using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus;

public interface IPipeConnectorSpecification :
    ISpecification
{
    void Connect(IPipeConnector connector);
}
