namespace ViciOne.ServiceBus.Configuration;

public interface IPublishPipeSpecificationObserver
{
    void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class;
}
