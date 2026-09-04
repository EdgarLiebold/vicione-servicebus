
namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Result of atomically admitting a durable send intent.</summary>
public readonly record struct DurableSendAdmissionResult(
    DurableSendId Id,
    DurableSendAdmissionDisposition Disposition,
    int StoredCount,
    long StoredBytes)
{
    /// <summary>
    /// Gets the is new value.
    /// </summary>
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
