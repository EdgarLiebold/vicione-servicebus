using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.RabbitMq.Testing;

var services = new ServiceCollection();
IServiceCollection configured = services.ConfigureRabbitMqTestOptions(options =>
{
    options.CleanVirtualHost = false;
    options.CreateVirtualHostIfNotExists = false;
});

if (!ReferenceEquals(services, configured) || services.Count == 0)
    throw new InvalidOperationException("RabbitMQ test options were not registered.");

if (typeof(RabbitMqTestHarnessOptions).Assembly.GetName().Name != "ViciOne.ServiceBus.RabbitMq.Testing")
    throw new InvalidOperationException("The RabbitMQ testing package assembly was not loaded.");
