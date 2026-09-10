namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates and connects the message pipelines discovered for an existing consumer instance.</summary>
public interface IInstanceConnector
{
    /// <summary>Creates an independent specification for the connector's discovered message contracts.</summary>
    /// <typeparam name="TConsumer">The consumer implementation configured by the specification.</typeparam>
    /// <returns>A new consumer specification.</returns>
    IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
        where TConsumer : class;

    /// <summary>Connects every discovered consumer contract of a runtime object instance.</summary>
    /// <param name="pipeConnector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="instance">The consumer instance to invoke.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, object instance);

    /// <summary>Connects every discovered message contract for a strongly typed consumer instance.</summary>
    /// <typeparam name="TConsumer">The consumer implementation to connect.</typeparam>
    /// <param name="pipeConnector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="instance">The consumer instance to invoke.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInstance<TConsumer>(IConsumePipeConnector pipeConnector, TConsumer instance,
        IConsumerSpecification<TConsumer> specification)
        where TConsumer : class;
}
