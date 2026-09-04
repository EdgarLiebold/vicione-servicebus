namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a batch consumer convention implementation.
/// </summary>
public class BatchConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new BatchConsumerMessageConvention<T>();
    }
}
