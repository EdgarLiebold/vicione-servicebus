namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates a message-discovery convention for a consumer type.</summary>
public interface IConsumerConvention
{
    /// <summary>Creates the convention that discovers message contracts for a consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer type to inspect.</typeparam>
    /// <returns>The consumer message convention.</returns>
    IConsumerMessageConvention GetConsumerMessageConvention<TConsumer>()
        where TConsumer : class;
}
