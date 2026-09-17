namespace ViciOne.ServiceBus.Sagas;

/// <summary>Creates the consume filter that combines a saga repository, policy, and saga pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <param name="repository">The repository used to locate and persist saga state.</param>
/// <param name="policy">The policy governing existing and missing instances.</param>
/// <param name="sagaPipe">The pipeline invoked for selected saga instances.</param>
/// <returns>The consume filter that dispatches messages through the supplied collaborators.</returns>
public delegate IFilter<ConsumeContext<TData>> SagaFilterFactory<TInstance, TData>(ISagaRepository<TInstance> repository,
    ISagaPolicy<TInstance, TData> policy, IPipe<SagaConsumeContext<TInstance, TData>> sagaPipe)
    where TInstance : class, ISaga
    where TData : class;
