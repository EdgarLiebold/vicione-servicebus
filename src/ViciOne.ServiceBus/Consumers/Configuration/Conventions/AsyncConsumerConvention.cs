namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for async consumer.</summary>
public class AsyncConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new AsyncConsumerMessageConvention<T>();
    }
}
