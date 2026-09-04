namespace ViciOne.ServiceBus.Metadata;

public interface IImplementedMessageType
{
    void ImplementsMessageType<T>(bool direct)
        where T : class;
}
