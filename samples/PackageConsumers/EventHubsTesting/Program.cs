using ViciOne.ServiceBus.EventHubs.Testing;

Type extensionType = typeof(EventHubTestHarnessExtensions);
if (extensionType.Assembly.GetName().Name != "ViciOne.ServiceBus.EventHubs.Testing")
    throw new InvalidOperationException("The Event Hubs testing package assembly was not loaded.");

if (extensionType.GetMethods().Count(static method => method.Name == "GetProducerAsync") != 1)
    throw new InvalidOperationException("The Event Hubs testing package did not expose its producer extension exactly once.");
