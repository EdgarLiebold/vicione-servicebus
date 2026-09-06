namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class InMemoryReliableInboxScope<TBus>
    where TBus : class, IBus;
