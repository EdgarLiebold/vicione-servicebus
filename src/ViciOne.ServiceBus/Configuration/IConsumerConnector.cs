namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates and connects the message pipelines discovered for a consumer type.</summary>
public interface IConsumerConnector
{
    /// <summary>Creates an independent specification for the connector's discovered message contracts.</summary>
    /// <typeparam name="TConsumer">The consumer implementation configured by the specification.</typeparam>
    /// <returns>A new consumer specification.</returns>
    IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
        where TConsumer : class;

    /// <summary>Connects every discovered message contract for a factory-created consumer.</summary>
    /// <typeparam name="TConsumer">The consumer implementation supplied by the factory.</typeparam>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="consumerFactory">The factory that supplies a consumer for each delivery.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumer<TConsumer>(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
        where TConsumer : class;
}
