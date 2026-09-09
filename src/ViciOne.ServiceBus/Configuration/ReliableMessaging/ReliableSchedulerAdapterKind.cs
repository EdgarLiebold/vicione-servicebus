namespace ViciOne.ServiceBus.Configuration;

internal enum ReliableSchedulerAdapterKind
{
    /// <summary>Scheduling is persisted by the reliable store.</summary>
    Store,

    /// <summary>Scheduling uses the selected transport's native delay capability.</summary>
    Transport,

    /// <summary>Scheduling is delegated to an explicitly addressed scheduler endpoint.</summary>
    Endpoint,
}
