using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SagaStateMachine;

internal sealed class StateMachineGraphVisitor<TSaga> :
    StateMachineVisitor
    where TSaga : class, SagaStateMachineInstance
{
    readonly HashSet<StateMachineGraphEdge> _edges;
    readonly Dictionary<(StateMachineGraphNode State, Event Event), StateMachineGraphNode> _eventBindings;
    readonly StateMachine<TSaga> _machine;
    readonly List<StateMachineGraphNode> _nonStateNodes;
    readonly List<StateMachineGraphEdge> _orderedEdges;
    readonly List<StateMachineGraphNode> _stateNodes;
    readonly Dictionary<string, StateMachineGraphNode> _states;
    readonly HashSet<Event> _visitedEvents;
    StateMachineGraphNode? _currentEvent;
    StateMachineGraphNode? _currentState;

    internal StateMachineGraphVisitor(StateMachine<TSaga> machine)
    {
        _machine = machine;

        _edges = new HashSet<StateMachineGraphEdge>();
        _states = new Dictionary<string, StateMachineGraphNode>(StringComparer.Ordinal);
        _eventBindings = new Dictionary<(StateMachineGraphNode, Event), StateMachineGraphNode>();
        _nonStateNodes = [];
        _orderedEdges = [];
        _stateNodes = [];
        _visitedEvents = new HashSet<Event>();
    }

    internal StateMachineGraph Graph
    {
        get
        {
            foreach (State state in _machine.States)
                GetStateNode(state);

            foreach (Event @event in _machine.Events)
            {
                if (_visitedEvents.Add(@event))
                    _nonStateNodes.Add(CreateEventNode(@event));
            }

            return new StateMachineGraph(_stateNodes.Concat(_nonStateNodes), _orderedEdges);
        }
    }

    public void Visit(State state, Action<State> next)
    {
        _currentState = GetStateNode(state);
        State<TSaga> typedState = _machine.GetState(state.Name);

        foreach (Event @event in typedState.DeclaredEvents)
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

    public void Visit(Event @event, Action<Event> next)
    {
        _currentEvent = GetEventNode(@event);
        AddEdge(new StateMachineGraphEdge(CurrentState, CurrentEvent, StateMachineGraphEdgeKind.EventBinding));

        next(@event);
    }

    public void Visit<TData>(Event<TData> @event, Action<Event<TData>> next)
        where TData : class
    {
        _currentEvent = GetEventNode(@event);
        AddEdge(new StateMachineGraphEdge(CurrentState, CurrentEvent, StateMachineGraphEdgeKind.EventBinding));

        next(@event);
    }

    public void Visit(IStateMachineActivity activity)
    {
        Visit(activity, x =>
        {
        });
    }

    public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next)
    {
        StateMachineGraphNode previousEvent = CurrentEvent;
        _currentEvent = CreateExceptionNode(activity.ExceptionType);

        AddEdge(new StateMachineGraphEdge(previousEvent, CurrentEvent, StateMachineGraphEdgeKind.ExceptionHandler));

        next(activity);

        _currentEvent = previousEvent;
    }

    public void Visit<T>(IBehavior<T> behavior)
        where T : class, SagaStateMachineInstance
    {
        Visit(behavior, x =>
        {
        });
    }

    public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
        where T : class, SagaStateMachineInstance
    {
        next(behavior);
    }

    public void Visit<T, TData>(IBehavior<T, TData> behavior)
        where T : class, SagaStateMachineInstance
        where TData : class
    {
        Visit(behavior, x =>
        {
        });
    }

    public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
        where T : class, SagaStateMachineInstance
        where TData : class
    {
        next(behavior);
    }

    public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
    {
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

    StateMachineGraphNode GetStateNode(State state)
    {
        if (_states.TryGetValue(state.Name, out StateMachineGraphNode? node))
            return node;

        node = CreateStateNode(state);
        _states.Add(state.Name, node);
        _stateNodes.Add(node);

        return node;
    }

    StateMachineGraphNode GetEventNode(Event @event)
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

    static StateMachineGraphNode CreateStateNode(State state) => StateMachineGraphNode.CreateState(state.Name);

    StateMachineGraphNode CreateEventNode(Event @event)
    {
        var targetType = @event
            .GetType()
            .GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == typeof(Event<>))
            .Select(x => x.GetGenericArguments()[0])
            .DefaultIfEmpty(typeof(Event))
            .Single();

        Type? messageType = targetType == typeof(Event) ? null : targetType;
        return StateMachineGraphNode.CreateEvent(@event.Name, messageType, _machine.IsCompositeEvent(@event));
    }

}
