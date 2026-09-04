namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an async consumer convention implementation.
/// </summary>
public class AsyncConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new AsyncConsumerMessageConvention<T>();
    }
}
