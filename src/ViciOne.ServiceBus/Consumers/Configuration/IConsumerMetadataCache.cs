namespace ViciOne.ServiceBus.Configuration
{
    public interface IConsumerMetadataCache<T>
    {
        IMessageInterfaceType[] ConsumerTypes { get; }
    }
}
