using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for state machine saga test harness.
/// </summary>
public static class StateMachineSagaTestHarnessExtensions
{
    /// <summary>
    /// Performs the state machine saga operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaStateMachineTestHarness<TStateMachine, TInstance> StateMachineSaga<TInstance, TStateMachine>(this BusTestHarness harness,
        TStateMachine stateMachine, string? queueName = null)
        where TInstance : class, SagaStateMachineInstance
        where TStateMachine : SagaStateMachine<TInstance>
    {
        if (stateMachine == null)
            throw new ArgumentNullException(nameof(stateMachine));

        var repository = new InMemorySagaRepository<TInstance>();

        return new StateMachineSagaTestHarness<TInstance, TStateMachine>(harness, repository, repository, repository, stateMachine, queueName);
    }

    /// <summary>
    /// Performs the state machine saga operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaStateMachineTestHarness<TStateMachine, TInstance> StateMachineSaga<TInstance, TStateMachine>(this BusTestHarness harness,
        TStateMachine stateMachine, ISagaRepository<TInstance> repository, string? queueName = null)
        where TInstance : class, SagaStateMachineInstance
        where TStateMachine : SagaStateMachine<TInstance>
    {
        if (stateMachine == null)
            throw new ArgumentNullException(nameof(stateMachine));

        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        var querySagaRepository = repository as IQuerySagaRepository<TInstance>;
        var loadSagaRepository = repository as ILoadSagaRepository<TInstance>;
        return new StateMachineSagaTestHarness<TInstance, TStateMachine>(harness, repository, querySagaRepository, loadSagaRepository, stateMachine,
            queueName);
    }

    /// <summary>
    /// Performs the contains in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="sagas">The sagas value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="stateSelector">The state selector value.</param>
    /// <returns>The result of the operation.</returns>
    public static TInstance? ContainsInState<TStateMachine, TInstance>(this ISagaList<TInstance> sagas, Guid correlationId, TStateMachine machine,
        Func<TStateMachine, State> stateSelector)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        var state = stateSelector(machine);

        return ContainsInState(sagas, correlationId, machine, state);
    }

    /// <summary>
    /// Performs the contains in state operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sagas">The sagas value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="state">The state value.</param>
    /// <returns>The result of the operation.</returns>
    public static T? ContainsInState<T>(this ISagaList<T> sagas, Guid correlationId, SagaStateMachine<T> machine, State state)
        where T : class, SagaStateMachineInstance
    {
        Func<T, bool> filter = machine.CreateSagaFilter(x => x.CorrelationId == correlationId, state);

        var any = sagas.Select(x => filter(x)).Any();
        return any ? sagas.Contains(correlationId) : null;
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="stateSelector">The state selector value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository, Guid correlationId,
        TStateMachine machine, Func<TStateMachine, State> stateSelector, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return ShouldContainSagaInStateAsync(repository, correlationId, machine, stateSelector, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="stateSelector">The state selector value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository, Guid correlationId,
        TStateMachine machine, Func<TStateMachine, State> stateSelector, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        var state = stateSelector(machine);

        return ShouldContainSagaInStateAsync(repository, correlationId, machine, state, timeout, timeProvider, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository, Guid correlationId,
        TStateMachine machine, State state, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return ShouldContainSagaInStateAsync(repository, correlationId, machine, state, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository, Guid correlationId,
        TStateMachine machine, State state, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return ShouldContainSagaInStateAsync(repository, x => x.CorrelationId == correlationId, machine, state, timeout, timeProvider, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="expression">The expression value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="stateSelector">The state selector value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> expression, TStateMachine machine, Func<TStateMachine, State> stateSelector, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return ShouldContainSagaInStateAsync(repository, expression, machine, stateSelector, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="expression">The expression value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="stateSelector">The state selector value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> expression, TStateMachine machine, Func<TStateMachine, State> stateSelector, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        var state = stateSelector(machine);

        return ShouldContainSagaInStateAsync(repository, expression, machine, state, timeout, timeProvider, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="expression">The expression value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> expression, TStateMachine machine, State state, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return await ShouldContainSagaInStateAsync(repository, expression, machine, state, timeout, TimeProvider.System, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the should contain saga in state operation.
    /// </summary>
    /// <typeparam name="TStateMachine">The t state machine type.</typeparam>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="repository">The repository value.</param>
    /// <param name="expression">The expression value.</param>
    /// <param name="machine">The machine value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<Guid?> ShouldContainSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> expression, TStateMachine machine, State state, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var querySagaRepository = repository as IQuerySagaRepository<TInstance>;
        if (querySagaRepository == null)
            throw new ArgumentException("The repository must support querying", nameof(repository));

        if (timeout <= TimeSpan.Zero)
            return default;

        var startedAt = timeProvider.GetTimestamp();

        ISagaQuery<TInstance> query = machine.CreateSagaQuery(expression, state);

        while (timeProvider.GetElapsedTime(startedAt) < timeout)
        {
            var saga = (await querySagaRepository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).FirstOrDefault();
            if (saga != Guid.Empty)
                return saga;

            await Task.Delay(TimeSpan.FromMilliseconds(10), timeProvider, cancellationToken).ConfigureAwait(false);
        }

        return default;
    }
}
