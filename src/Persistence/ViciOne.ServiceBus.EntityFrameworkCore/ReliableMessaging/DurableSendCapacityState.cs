namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class DurableSendCapacityState
{
    public required string StoreKey { get; set; }
    public int StoredCount { get; set; }
    public long StoredBytes { get; set; }
}
