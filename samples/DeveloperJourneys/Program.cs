using Microsoft.Extensions.DependencyInjection;

using ViciOne.ServiceBus.Samples.DeveloperJourneys;

var services = new ServiceCollection();
Journey13UnitTestHarness.ConfigureProviderHarnesses(services);

string[] expectedAssemblies =
[
    "ViciOne.ServiceBus.AzureServiceBus.Testing",
    "ViciOne.ServiceBus.EventHubs.Testing",
    "ViciOne.ServiceBus.RabbitMq.Testing",
];

if (!expectedAssemblies.SequenceEqual(Journey13UnitTestHarness.ProviderTestingAssemblies(), StringComparer.Ordinal))
    throw new InvalidOperationException("The package-only provider testing assemblies were not loaded exactly.");

if (services.Count == 0)
    throw new InvalidOperationException("The package-only provider testing configuration registered no services.");
