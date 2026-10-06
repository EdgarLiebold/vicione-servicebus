namespace ViciOne.ServiceBus.Configuration;

/// <summary>Carries the owning registration's stable diagnostic key without retaining its services.</summary>
internal sealed class ConsumerBusIdentityOptions(string busKey) : IOptions
{
    internal string BusKey { get; } = busKey;
}
