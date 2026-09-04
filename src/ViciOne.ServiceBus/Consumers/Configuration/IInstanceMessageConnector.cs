using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for instance message connector.
/// </summary>
public interface IInstanceMessageConnector
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }
}


/// <summary>
/// Defines the contract for instance message connector.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public interface IInstanceMessageConnector<TInstance> :
    IInstanceMessageConnector
    where TInstance : class
{
    /// <summary>
    /// Creates consumer message specification.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IConsumerMessageSpecification<TInstance> CreateConsumerMessageSpecification();

    /// <summary>
    /// Connects instance.
    /// </summary>
    /// <param name="pipeConnector">The pipe connector value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, TInstance instance, IConsumerSpecification<TInstance> specification);
}
