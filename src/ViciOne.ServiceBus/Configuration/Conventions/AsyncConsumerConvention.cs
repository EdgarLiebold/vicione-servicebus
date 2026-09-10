namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers message contracts declared through <see cref="IConsumer{TMessage}" />.</summary>
public sealed class AsyncConsumerConvention :
    IConsumerConvention
{
    IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<TConsumer>()
    {
        return new AsyncConsumerMessageConvention<TConsumer>();
    }
}
