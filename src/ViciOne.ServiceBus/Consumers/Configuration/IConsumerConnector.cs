namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Interface implemented by objects that tie an inbound pipeline together with
/// consumers (by means of calling a consumer factory).
/// </summary>
public interface IConsumerConnector
{
    /// <summary>
    /// Creates consumer specification.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
        where TConsumer : class;

    /// <summary>
    /// Connects consumer.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumer<TConsumer>(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
        where TConsumer : class;
}
