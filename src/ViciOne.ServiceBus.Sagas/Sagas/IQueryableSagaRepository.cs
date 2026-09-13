namespace ViciOne.ServiceBus.Sagas;

/// <summary>Combines message dispatch, direct loading, and repository queries for saga state.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
public interface IQueryableSagaRepository<TSaga> :
    ILoadableSagaRepository<TSaga>,
    IQuerySagaRepository<TSaga>
    where TSaga : class, ISaga;
