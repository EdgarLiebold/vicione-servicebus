namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers strongly typed job contracts implemented by a consumer.</summary>
internal sealed class JobConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
    {
        return new JobConsumerMessageConvention<T>();
    }
}
