namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for instance connector.
/// </summary>
public interface IInstanceConnector
{
    /// <summary>
    /// Creates consumer specification.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IConsumerSpecification<TConsumer> CreateConsumerSpecification<TConsumer>()
        where TConsumer : class;

    /// <summary>
    /// Connects instance.
    /// </summary>
    /// <param name="pipeConnector">The pipe connector value.</param>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, object instance);

    /// <summary>
    /// Connects instance.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="pipeConnector">The pipe connector value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectInstance<TInstance>(IConsumePipeConnector pipeConnector, TInstance instance,
        IConsumerSpecification<TInstance> specification)
        where TInstance : class;
}
