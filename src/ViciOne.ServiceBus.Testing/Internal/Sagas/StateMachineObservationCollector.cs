using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

sealed class StateMachineObservationCollector<TInstance> :
    IEventObserver<TInstance>,
    IStateObserver<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly TestObservationList<StateMachineEventObservation> _events;
    readonly TestObservationList<StateMachineStateChange> _stateChanges;

    public StateMachineObservationCollector(TestContextSaveMode saveMode, int maximumSavedElements)
    {
        _events = new TestObservationList<StateMachineEventObservation>(saveMode, maximumSavedElements);
        _stateChanges = new TestObservationList<StateMachineStateChange>(saveMode, maximumSavedElements);
    }

    public IReadOnlyList<StateMachineEventObservation> Events => _events.Snapshot();
    public IReadOnlyList<StateMachineStateChange> StateChanges => _stateChanges.Snapshot();

    public Task PreExecuteAsync(IBehaviorContext<TInstance> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        AddEvent(context, null, StateMachineEventExecutionStatus.Started);
        return Task.CompletedTask;
    }

    public Task PreExecuteAsync<TMessage>(IBehaviorContext<TInstance, TMessage> context)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        AddEvent(context, typeof(TMessage), StateMachineEventExecutionStatus.Started);
        return Task.CompletedTask;
    }

    public Task PostExecuteAsync(IBehaviorContext<TInstance> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        AddEvent(context, null, StateMachineEventExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task PostExecuteAsync<TMessage>(IBehaviorContext<TInstance, TMessage> context)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        AddEvent(context, typeof(TMessage), StateMachineEventExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task ExecuteFaultAsync(IBehaviorContext<TInstance> context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        AddEvent(context, null, StateMachineEventExecutionStatus.Faulted, exception);
        return Task.CompletedTask;
    }

    public Task ExecuteFaultAsync<TMessage>(IBehaviorContext<TInstance, TMessage> context, Exception exception)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        AddEvent(context, typeof(TMessage), StateMachineEventExecutionStatus.Faulted, exception);
        return Task.CompletedTask;
    }

    public Task StateChangedAsync(IBehaviorContext<TInstance> context, IState currentState, IState? previousState)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentState);
        _stateChanges.Add(new StateMachineStateChange(context.Saga.CorrelationId, previousState?.Name, currentState.Name));
        return Task.CompletedTask;
    }

    void AddEvent(IBehaviorContext<TInstance> context, Type? dataType, StateMachineEventExecutionStatus status, Exception? exception = null)
    {
        _events.Add(new StateMachineEventObservation(context.Saga.CorrelationId, context.Event.Name, dataType, status, exception));
    }
}
