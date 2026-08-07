// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public interface ISagaMessageConnector
    {
        Type MessageType { get; }
    }


    public interface ISagaMessageConnector<TSaga> :
        ISagaMessageConnector
        where TSaga : class, ISaga
    {
        ISagaMessageSpecification<TSaga> CreateSagaMessageSpecification();

        ConnectHandle ConnectSaga(IConsumePipeConnector consumePipe, ISagaRepository<TSaga> repository, ISagaSpecification<TSaga> specification);
    }
}
