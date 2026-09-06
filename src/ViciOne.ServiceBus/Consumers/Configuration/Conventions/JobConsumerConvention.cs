namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for job consumer.</summary>
public class JobConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new JobConsumerMessageConvention<T>();
    }
}
