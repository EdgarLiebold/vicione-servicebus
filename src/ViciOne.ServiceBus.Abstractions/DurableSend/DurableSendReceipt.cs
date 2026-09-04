namespace ViciOne.ServiceBus;

/// <summary>
/// Receipt proving that one typed send intent crossed the producer-side durable persistence boundary.
/// It is deliberately not a transport-delivery or consumer-completion receipt.
/// </summary>
public readonly record struct DurableSendReceipt(
    DurableSendId Id,
    DurableSendAdmissionDisposition Disposition,
    int StoredCount,
    long StoredBytes)
{
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
