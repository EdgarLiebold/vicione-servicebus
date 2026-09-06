namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for batch consumer.</summary>
public class BatchConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new BatchConsumerMessageConvention<T>();
    }
}
