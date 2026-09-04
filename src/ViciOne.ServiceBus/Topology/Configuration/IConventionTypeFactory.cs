namespace ViciOne.ServiceBus.Configuration;

public interface IConventionTypeFactory<out TValue>
    where TValue : class
{
    TValue Create<T>()
        where T : class;
}
