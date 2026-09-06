using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBus.Testing;

var services = new ServiceCollection();
IServiceCollection configured = services.ConfigureServiceBusTestOptions(options => options.CleanNamespace = false);

if (!ReferenceEquals(services, configured) || services.Count == 0)
    throw new InvalidOperationException("Azure Service Bus test options were not registered.");

if (typeof(AzureServiceBusTestHarnessOptions).Assembly.GetName().Name
    != "ViciOne.ServiceBus.AzureServiceBus.Testing")
    throw new InvalidOperationException("The Azure Service Bus testing package assembly was not loaded.");
