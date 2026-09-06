namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Interface implemented by objects that tie an inbound pipeline together with
/// consumers (by means of calling a consumer factory).
/// </summary>
public interface IConsumerConnector
{
    /// <summary>Creates consumer specification.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <returns>The created consumer specification.</returns>
    IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
        where TConsumer : class;

    /// <summary>Connects consumer.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumer<TConsumer>(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
        where TConsumer : class;
}
