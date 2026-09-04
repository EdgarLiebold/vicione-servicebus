namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for transport sequence number.
/// </summary>
public interface ITransportSequenceNumber
{
    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    ulong? SequenceNumber { get; }
}
