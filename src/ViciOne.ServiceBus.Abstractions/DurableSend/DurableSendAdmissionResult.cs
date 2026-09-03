namespace ViciOne.ServiceBus;

/// <summary>Result of atomically admitting a durable send intent.</summary>
public readonly record struct DurableSendAdmissionResult(
    DurableSendId Id,
    DurableSendAdmissionDisposition Disposition,
    int StoredCount,
    long StoredBytes)
{
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
