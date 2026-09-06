namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles saga filter factory.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <param name="repository">The repository.</param>
/// <param name="policy">The policy.</param>
/// <param name="sagaPipe">The saga pipe.</param>
/// <returns>The value produced by the operation.</returns>
public delegate IFilter<ConsumeContext<TData>> SagaFilterFactory<TInstance, TData>(ISagaRepository<TInstance> repository,
    ISagaPolicy<TInstance, TData> policy, IPipe<SagaConsumeContext<TInstance, TData>> sagaPipe)
    where TInstance : class, ISaga
    where TData : class;
