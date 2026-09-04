namespace ViciOne.ServiceBus;

public interface ITransportSequenceNumber
{
    ulong? SequenceNumber { get; }
}
