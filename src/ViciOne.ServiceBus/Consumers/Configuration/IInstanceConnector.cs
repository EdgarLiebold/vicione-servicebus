namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by instance connector.</summary>
public interface IInstanceConnector
{
    /// <summary>Creates consumer specification.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <returns>The created consumer specification.</returns>
    IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
        where TConsumer : class;

    /// <summary>Connects instance.</summary>
    /// <param name="pipeConnector">The pipe connector.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, object instance);

    /// <summary>Connects instance.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="pipeConnector">The pipe connector.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInstance<TInstance>(IConsumePipeConnector pipeConnector, TInstance instance,
        IConsumerSpecification<TInstance> specification)
        where TInstance : class;
}
