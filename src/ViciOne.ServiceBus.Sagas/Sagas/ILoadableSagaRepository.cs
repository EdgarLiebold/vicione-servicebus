using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Combines message dispatch with direct loading by saga correlation identifier.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
public interface ILoadableSagaRepository<TSaga> :
    ISagaRepository<TSaga>,
    ILoadSagaRepository<TSaga>
    where TSaga : class, ISaga;
