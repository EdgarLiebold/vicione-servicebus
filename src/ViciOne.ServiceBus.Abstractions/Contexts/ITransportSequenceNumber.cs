namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by transport sequence number.</summary>
public interface ITransportSequenceNumber
{
    /// <summary>Gets the sequence number.</summary>
    ulong? SequenceNumber { get; }
}
