// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.UsageTracking;

public interface IUsageTracker
{
    public void PreConfigureBus<T>(T configurator, IBusRegistrationContext context)
        where T : IBusFactoryConfigurator;

    void PreConfigureRider<T>(T configurator)
        where T : IRiderFactoryConfigurator;
}
