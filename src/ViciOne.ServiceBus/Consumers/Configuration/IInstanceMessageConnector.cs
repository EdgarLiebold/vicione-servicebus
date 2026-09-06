using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by instance message connector.</summary>
public interface IInstanceMessageConnector
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }
}


/// <summary>Defines the operations required by instance message connector.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public interface IInstanceMessageConnector<TInstance> :
    IInstanceMessageConnector
    where TInstance : class
{
    /// <summary>Creates consumer message specification.</summary>
    /// <returns>The created consumer message specification.</returns>
    IConsumerMessageSpecification<TInstance> CreateConsumerMessageSpecification();

    /// <summary>Connects instance.</summary>
    /// <param name="pipeConnector">The pipe connector.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, TInstance instance, IConsumerSpecification<TInstance> specification);
}
