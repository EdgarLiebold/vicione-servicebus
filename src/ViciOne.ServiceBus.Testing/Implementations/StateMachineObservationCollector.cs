#nullable enable
namespace ViciOne.ServiceBus.Testing.Implementations;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    public Task PreExecute(BehaviorContext<TInstance> context)
    {
        AddEvent(context, null, StateMachineEventExecutionStatus.Started);
        return Task.CompletedTask;
    }

    public Task PreExecute<T>(BehaviorContext<TInstance, T> context)
        where T : class
    {
        AddEvent(context, typeof(T), StateMachineEventExecutionStatus.Started);
        return Task.CompletedTask;
    }

    public Task PostExecute(BehaviorContext<TInstance> context)
    {
        AddEvent(context, null, StateMachineEventExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task PostExecute<T>(BehaviorContext<TInstance, T> context)
        where T : class
    {
        AddEvent(context, typeof(T), StateMachineEventExecutionStatus.Completed);
        return Task.CompletedTask;
    }

    public Task ExecuteFault(BehaviorContext<TInstance> context, Exception exception)
    {
        AddEvent(context, null, StateMachineEventExecutionStatus.Faulted, exception);
        return Task.CompletedTask;
    }

    public Task ExecuteFault<T>(BehaviorContext<TInstance, T> context, Exception exception)
        where T : class
    {
        AddEvent(context, typeof(T), StateMachineEventExecutionStatus.Faulted, exception);
        return Task.CompletedTask;
    }

    public Task StateChanged(BehaviorContext<TInstance> context, State currentState, State previousState)
    {
        _stateChanges.Add(new StateMachineStateChange(context.Saga.CorrelationId, previousState?.Name, currentState.Name));
        return Task.CompletedTask;
    }

    void AddEvent(BehaviorContext<TInstance> context, Type? dataType, StateMachineEventExecutionStatus status, Exception? exception = null)
    {
        _events.Add(new StateMachineEventObservation(context.Saga.CorrelationId, context.Event.Name, dataType, status, exception));
    }
}
