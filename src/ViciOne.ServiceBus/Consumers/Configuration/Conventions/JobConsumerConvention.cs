namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job consumer convention implementation.
/// </summary>
public class JobConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new JobConsumerMessageConvention<T>();
    }
}
