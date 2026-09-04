namespace ViciOne.ServiceBus;

public interface IInMemoryPublishTopology :
    IPublishTopology
{
    new IInMemoryMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;
}
