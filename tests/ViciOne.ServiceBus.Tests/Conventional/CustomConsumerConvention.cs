// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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
