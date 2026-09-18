using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SagaStateMachine;

internal sealed class StateMachineGraphVisitor<TSaga> :
    IStateMachineVisitor
    where TSaga : class, ISagaStateMachineInstance
{
    readonly HashSet<StateMachineGraphEdge> _edges;
    readonly Dictionary<(StateMachineGraphNode State, IEvent Event), StateMachineGraphNode> _eventBindings;
    readonly IStateMachine<TSaga> _machine;
    readonly List<StateMachineGraphNode> _nonStateNodes;
    readonly List<StateMachineGraphEdge> _orderedEdges;
    readonly List<StateMachineGraphNode> _stateNodes;
    readonly Dictionary<string, StateMachineGraphNode> _states;
    readonly HashSet<IEvent> _visitedEvents;
    StateMachineGraphNode? _currentEvent;
    StateMachineGraphNode? _currentState;
    int _eventVisitDepth;
    int _exceptionVisitDepth;

    internal StateMachineGraphVisitor(IStateMachine<TSaga> machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        _machine = machine;

        _edges = new HashSet<StateMachineGraphEdge>();
        _states = new Dictionary<string, StateMachineGraphNode>(StringComparer.Ordinal);
        _eventBindings = new Dictionary<(StateMachineGraphNode, IEvent), StateMachineGraphNode>();
        _nonStateNodes = [];
        _orderedEdges = [];
        _stateNodes = [];
        _visitedEvents = new HashSet<IEvent>();
    }

    internal StateMachineGraph Graph
    {
        get
        {
            foreach (IState state in _machine.States)
                GetStateNode(state);

            foreach (IEvent @event in _machine.Events)
            {
                if (_visitedEvents.Add(@event))
                    _nonStateNodes.Add(CreateEventNode(@event));
            }

            return new StateMachineGraph(_stateNodes.Concat(_nonStateNodes), _orderedEdges);
        }
    }

    public void Visit(IState state, Action<IState> next)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(next);

        StateMachineGraphNode? previousState = _currentState;
        StateMachineGraphNode? previousEvent = _currentEvent;
        _currentState = GetStateNode(state);
        try
        {
            IState<TSaga> typedState = _machine.GetState(state.Name);

            foreach (IEvent @event in typedState.DeclaredEvents)
            {
                StateMachineGraphNode eventNode = GetEventNode(@event);
                AddEdge(new StateMachineGraphEdge(CurrentState, eventNode, StateMachineGraphEdgeKind.EventBinding));
            }

            if (typedState.SuperState is not null)
            {
                StateMachineGraphNode superState = GetStateNode(typedState.SuperState);
                AddEdge(new StateMachineGraphEdge(CurrentState, superState, StateMachineGraphEdgeKind.StateInheritance));
            }

            next(state);
        }
        finally
        {
            _currentState = previousState;
            _currentEvent = previousEvent;
        }
    }

    public void Visit(IEvent @event, Action<IEvent> next)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(next);

        StateMachineGraphNode? previousEvent = _currentEvent;
        bool restoreEvent = _eventVisitDepth > 0 || _exceptionVisitDepth > 0;
        _currentEvent = GetEventNode(@event);
        _eventVisitDepth++;
        try
        {
            AddEdge(new StateMachineGraphEdge(CurrentState, CurrentEvent, StateMachineGraphEdgeKind.EventBinding));
            next(@event);
        }
        catch
        {
            restoreEvent = true;
            throw;
        }
        finally
        {
            _eventVisitDepth--;
            if (restoreEvent)
                _currentEvent = previousEvent;
        }
    }

    public void Visit<TData>(IEvent<TData> @event, Action<IEvent<TData>> next)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(next);

        StateMachineGraphNode? previousEvent = _currentEvent;
        bool restoreEvent = _eventVisitDepth > 0 || _exceptionVisitDepth > 0;
        _currentEvent = GetEventNode(@event);
        _eventVisitDepth++;
        try
        {
            AddEdge(new StateMachineGraphEdge(CurrentState, CurrentEvent, StateMachineGraphEdgeKind.EventBinding));
            next(@event);
        }
        catch
        {
            restoreEvent = true;
            throw;
        }
        finally
        {
            _eventVisitDepth--;
            if (restoreEvent)
                _currentEvent = previousEvent;
        }
    }

    public void Visit(IStateMachineActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        Visit(activity, x =>
        {
        });
    }

    public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(next);

        StateMachineGraphNode previousEvent = CurrentEvent;
        _currentEvent = CreateExceptionNode(activity.ExceptionType);
        _exceptionVisitDepth++;
        try
        {
            AddEdge(new StateMachineGraphEdge(previousEvent, CurrentEvent, StateMachineGraphEdgeKind.ExceptionHandler));
            next(activity);
        }
        finally
        {
            _exceptionVisitDepth--;
            _currentEvent = previousEvent;
        }
    }

    public void Visit<T>(IBehavior<T> behavior)
        where T : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(behavior);

        Visit(behavior, x =>
        {
        });
    }

    public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
        where T : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(next);

        next(behavior);
    }

    public void Visit<T, TData>(IBehavior<T, TData> behavior)
        where T : class, ISagaStateMachineInstance
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(behavior);

        Visit(behavior, x =>
        {
        });
    }

    public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
        where T : class, ISagaStateMachineInstance
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(next);

        next(behavior);
    }

    public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(next);

        if (activity is TransitionActivity<TSaga> transitionActivity)
        {
            InspectTransitionActivity(transitionActivity);
            next(activity);
            return;
        }

        if (activity is CompositeEventActivity<TSaga> compositeActivity)
        {
            InspectCompositeEventActivity(compositeActivity);
            next(activity);
            return;
        }

        next(activity);
    }

    void InspectTransitionActivity(TransitionActivity<TSaga> transitionActivity)
    {
        StateMachineGraphNode targetState = GetStateNode(transitionActivity.ToState);

        AddEdge(new StateMachineGraphEdge(CurrentEvent, targetState, StateMachineGraphEdgeKind.StateTransition));
    }

    void InspectCompositeEventActivity(CompositeEventActivity<TSaga> compositeActivity)
    {
        StateMachineGraphNode compositeEvent = GetEventNode(compositeActivity.Event);

        AddEdge(new StateMachineGraphEdge(CurrentEvent, compositeEvent, StateMachineGraphEdgeKind.CompositeContribution));
    }

    StateMachineGraphNode GetStateNode(IState state)
    {
        if (_states.TryGetValue(state.Name, out StateMachineGraphNode? node))
            return node;

        node = CreateStateNode(state);
        _states.Add(state.Name, node);
        _stateNodes.Add(node);

        return node;
    }

    StateMachineGraphNode GetEventNode(IEvent @event)
    {
        var key = (CurrentState, @event);
        if (_eventBindings.TryGetValue(key, out StateMachineGraphNode? node))
            return node;

        node = CreateEventNode(@event);
        _eventBindings.Add(key, node);
        _visitedEvents.Add(@event);
        _nonStateNodes.Add(node);

        return node;
    }

    StateMachineGraphNode CreateExceptionNode(Type exceptionType)
    {
        StateMachineGraphNode node = StateMachineGraphNode.CreateException(exceptionType);
        _nonStateNodes.Add(node);

        return node;
    }

    StateMachineGraphNode CurrentEvent => _currentEvent
        ?? throw new InvalidOperationException("A state-machine event must be visited before its activities.");

    StateMachineGraphNode CurrentState => _currentState
        ?? throw new InvalidOperationException("A state-machine state must be visited before its events.");

    void AddEdge(StateMachineGraphEdge edge)
    {
        if (_edges.Add(edge))
            _orderedEdges.Add(edge);
    }

    static StateMachineGraphNode CreateStateNode(IState state) => StateMachineGraphNode.CreateState(state.Name);

    StateMachineGraphNode CreateEventNode(IEvent @event)
    {
        var targetType = @event
            .GetType()
            .GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == typeof(IEvent<>))
            .Select(x => x.GetGenericArguments()[0])
            .DefaultIfEmpty(typeof(IEvent))
            .Single();

        Type? messageType = targetType == typeof(IEvent) ? null : targetType;
        return StateMachineGraphNode.CreateEvent(@event.Name, messageType, _machine.IsCompositeEvent(@event));
    }

}
