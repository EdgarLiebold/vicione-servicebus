using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

public interface IRegistrationFilter
{
    bool Matches(IConsumerRegistration registration);
    bool Matches(ISagaRegistration registration);
    bool Matches(IExecuteActivityRegistration registration);
    bool Matches(IActivityRegistration registration);
    bool Matches(IFutureRegistration registration);
    bool Matches(IEndpointRegistration registration);
}
