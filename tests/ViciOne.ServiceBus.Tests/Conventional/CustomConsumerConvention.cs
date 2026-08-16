namespace ViciOne.ServiceBus.Tests.Conventional
{
    using ViciOne.ServiceBus.Configuration;


    class CustomConsumerConvention :
        IConsumerConvention
    {
        IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
        {
            return new CustomConsumerMessageConvention<T>();
        }
    }
}
