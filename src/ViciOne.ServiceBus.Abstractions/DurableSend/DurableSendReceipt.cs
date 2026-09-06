namespace ViciOne.ServiceBus;

/// <summary>
/// Receipt proving that one typed send intent crossed the producer-side durable persistence boundary.
/// It is deliberately not a transport-delivery or consumer-completion receipt.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Disposition">The disposition.</param>
/// <param name="StoredCount">The stored count.</param>
/// <param name="StoredBytes">The stored bytes.</param>
public readonly record struct DurableSendReceipt(
    DurableSendId Id,
    DurableSendAdmissionDisposition Disposition,
    int StoredCount,
    long StoredBytes)
{
    /// <summary>Gets a value indicating whether new.</summary>
    public bool IsNew => Disposition == DurableSendAdmissionDisposition.Accepted;
}
