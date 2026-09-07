using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by saga message connector.</summary>
public interface ISagaMessageConnector
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }
}


/// <summary>Defines the operations required by saga message connector.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaMessageConnector<TSaga> :
    ISagaMessageConnector
    where TSaga : class, ISaga
{
    /// <summary>Creates saga message specification.</summary>
    /// <returns>The created saga message specification.</returns>
    ISagaMessageSpecification<TSaga> CreateSagaMessageSpecification();

    /// <summary>Connects saga.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSaga(IConsumePipeConnector consumePipe, ISagaRepository<TSaga> repository, ISagaSpecification<TSaga> specification);
}
