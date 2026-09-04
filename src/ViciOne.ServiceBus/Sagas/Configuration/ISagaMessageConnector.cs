using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga message connector.
/// </summary>
public interface ISagaMessageConnector
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }
}


/// <summary>
/// Defines the contract for saga message connector.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaMessageConnector<TSaga> :
    ISagaMessageConnector
    where TSaga : class, ISaga
{
    /// <summary>
    /// Creates saga message specification.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ISagaMessageSpecification<TSaga> CreateSagaMessageSpecification();

    /// <summary>
    /// Connects saga.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectSaga(IConsumePipeConnector consumePipe, ISagaRepository<TSaga> repository, ISagaSpecification<TSaga> specification);
}
