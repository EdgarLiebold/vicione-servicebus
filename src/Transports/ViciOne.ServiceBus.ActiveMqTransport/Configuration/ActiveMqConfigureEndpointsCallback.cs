// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public delegate void ActiveMqConfigureEndpointsCallback(IRegistrationContext context, string queueName, IActiveMqReceiveEndpointConfigurator configurator);
