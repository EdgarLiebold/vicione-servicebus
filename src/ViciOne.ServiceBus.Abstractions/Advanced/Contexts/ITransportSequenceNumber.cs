namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the broker-assigned sequence number of a received message when available.</summary>
public interface ITransportSequenceNumber
{
    /// <summary>Gets the broker-assigned sequence number, or <see langword="null" /> when the transport does not provide one.</summary>
    ulong? SequenceNumber { get; }
}
