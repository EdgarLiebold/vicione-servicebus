namespace ViciOne.ServiceBus;

using System.ComponentModel;

/// <summary>Result of atomically admitting a durable send intent.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct DurableSendAdmissionResult(
    DurableSendId Id,
    DurableSendAdmissionDisposition Disposition,
    int StoredCount,
    long StoredBytes)
{
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
