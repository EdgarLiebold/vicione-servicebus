// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Saga;


    public interface ISagaMetadataCache<out TSaga>
        where TSaga : class, ISaga
    {
        SagaInterfaceType[] InitiatedByTypes { get; }
        SagaInterfaceType[] OrchestratesTypes { get; }
        SagaInterfaceType[] ObservesTypes { get; }
        SagaInterfaceType[] InitiatedByOrOrchestratesTypes { get; }

        SagaInstanceFactoryMethod<TSaga> FactoryMethod { get; }
    }
}
