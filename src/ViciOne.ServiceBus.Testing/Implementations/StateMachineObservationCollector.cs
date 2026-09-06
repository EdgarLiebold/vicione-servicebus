using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

sealed class StateMachineObservationCollector<TInstance> :
    IEventObserver<TInstance>,
    IStateObserver<TInstance>
    where TInstance : class, SagaStateMachineInstance
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

    public Task PreExecuteAsync(BehaviorContext<TInstance> context)
    {
        AddEvent(context, null, StateMachineEventExecutionStatus.Started);
        return Task.CompletedTask;
    }

    public Task PreExecuteAsync<T>(BehaviorContext<TInstance, T> context)
        where T : class
    {
        AddEvent(context, typeof(T), StateMachineEventExecutionStatus.Started);
        return Task.CompletedTask;
    }

    public Task PostExecuteAsync(BehaviorContext<TInstance> context)
    {
        AddEvent(context, null, StateMachineEventExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task PostExecuteAsync<T>(BehaviorContext<TInstance, T> context)
        where T : class
    {
        AddEvent(context, typeof(T), StateMachineEventExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task ExecuteFaultAsync(BehaviorContext<TInstance> context, Exception exception)
    {
        AddEvent(context, null, StateMachineEventExecutionStatus.Faulted, exception);
        return Task.CompletedTask;
    }

    public Task ExecuteFaultAsync<T>(BehaviorContext<TInstance, T> context, Exception exception)
        where T : class
    {
        AddEvent(context, typeof(T), StateMachineEventExecutionStatus.Faulted, exception);
        return Task.CompletedTask;
    }

    public Task StateChangedAsync(BehaviorContext<TInstance> context, State currentState, State? previousState)
    {
        _stateChanges.Add(new StateMachineStateChange(context.Saga.CorrelationId, previousState?.Name, currentState.Name));
        return Task.CompletedTask;
    }

    void AddEvent(BehaviorContext<TInstance> context, Type? dataType, StateMachineEventExecutionStatus status, Exception? exception = null)
    {
        _events.Add(new StateMachineEventObservation(context.Saga.CorrelationId, context.Event.Name, dataType, status, exception));
    }
}
