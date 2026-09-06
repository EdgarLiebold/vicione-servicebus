
namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Result of atomically admitting a durable send intent.</summary>
/// <param name="Id">The id.</param>
/// <param name="Disposition">The disposition.</param>
/// <param name="StoredCount">The stored count.</param>
/// <param name="StoredBytes">The stored bytes.</param>
public readonly record struct DurableSendAdmissionResult(
    DurableSendId Id,
    DurableSendAdmissionDisposition Disposition,
    int StoredCount,
    long StoredBytes)
{
    /// <summary>Gets a value indicating whether new.</summary>
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
