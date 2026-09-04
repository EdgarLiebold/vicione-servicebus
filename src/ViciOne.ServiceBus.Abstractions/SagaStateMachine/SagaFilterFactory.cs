namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles saga filter factory.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <param name="repository">The repository value.</param>
/// <param name="policy">The policy value.</param>
/// <param name="sagaPipe">The saga pipe value.</param>
/// <returns>The result of the operation.</returns>
public delegate IFilter<ConsumeContext<TData>> SagaFilterFactory<TInstance, TData>(ISagaRepository<TInstance> repository,
    ISagaPolicy<TInstance, TData> policy, IPipe<SagaConsumeContext<TInstance, TData>> sagaPipe)
    where TInstance : class, ISaga
    where TData : class;
