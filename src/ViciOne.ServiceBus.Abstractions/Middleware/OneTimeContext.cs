namespace ViciOne.ServiceBus;

public interface OneTimeContext<TPayload>
    where TPayload : class
{
    void Evict();
}
