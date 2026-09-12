namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers completed-batch contracts declared through <see cref="IConsumer{TMessage}" />.</summary>
internal sealed class BatchConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<TConsumer>()
    {
        return new BatchConsumerMessageConvention<TConsumer>();
    }
}
