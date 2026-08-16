namespace ViciOne.ServiceBus.Configuration
{
    public class BatchConsumerConvention :
        IConsumerConvention
    {
        IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
        {
            return new BatchConsumerMessageConvention<T>();
        }
    }
}
