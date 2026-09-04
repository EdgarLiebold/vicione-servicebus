namespace ViciOne.ServiceBus;

public interface ConsumerFaultContext
{
    string MessageType { get; }
    string ConsumerType { get; }
}
