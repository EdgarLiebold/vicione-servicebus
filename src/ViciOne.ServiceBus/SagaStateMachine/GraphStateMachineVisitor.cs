using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a graph state machine visitor implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class GraphStateMachineVisitor<TSaga> :
    StateMachineVisitor
    where TSaga : class, SagaStateMachineInstance
{
    readonly HashSet<Edge> _edges;
    readonly Dictionary<Event, Vertex> _events;
    readonly StateMachine<TSaga> _machine;
    readonly Dictionary<State, Vertex> _states;
    Edge? _currentEdge;
    Vertex? _currentEvent;
    Vertex? _currentState;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machine">The machine value.</param>
    public GraphStateMachineVisitor(StateMachine<TSaga> machine)
    {
        _machine = machine;

        _edges = new HashSet<Edge>();
        _states = new Dictionary<State, Vertex>();
        _events = new Dictionary<Event, Vertex>();
    }

    /// <summary>
    /// Gets the graph value.
    /// </summary>
    public StateMachineGraph Graph
    {
        get
        {
            IEnumerable<Vertex> events = _events.Values
                .Where(e => _edges.Any(edge => edge.From.Equals(e)));

            IEnumerable<Vertex> states = _states.Values
                .Where(s => _edges.Any(edge => edge.From.Equals(s) || edge.To.Equals(s)));

            var vertices = new HashSet<Vertex>(states.Union(events));

            IEnumerable<Edge> edges = _edges
                .Where(e => vertices.Contains(e.From) && vertices.Contains(e.To));

            return new StateMachineGraph(vertices, edges);
        }
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="next">The next value.</param>
    public void Visit(State state, Action<State> next)
    {
        _currentState = GetStateVertex(state);

        next(state);
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="next">The next value.</param>
    public void Visit(Event @event, Action<Event> next)
    {
        _currentEvent = GetEventVertex(@event);
        _currentEdge = null;

        next(@event);
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="next">The next value.</param>
    public void Visit<TData>(Event<TData> @event, Action<Event<TData>> next)
        where TData : class
    {
        _currentEvent = GetEventVertex(@event);
        _currentEdge = null;

        next(@event);
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    public void Visit(IStateMachineActivity activity)
    {
        Visit(activity, x =>
        {
        });
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    public void Visit<T>(IBehavior<T> behavior)
        where T : class, SagaStateMachineInstance
    {
        Visit(behavior, x =>
        {
        });
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    /// <param name="next">The next value.</param>
    public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
        where T : class, SagaStateMachineInstance
    {
        next(behavior);
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    public void Visit<T, TData>(IBehavior<T, TData> behavior)
        where T : class, SagaStateMachineInstance
        where TData : class
    {
        Visit(behavior, x =>
        {
        });
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    /// <param name="next">The next value.</param>
    public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
        where T : class, SagaStateMachineInstance
        where TData : class
    {
        next(behavior);
    }

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <param name="next">The next value.</param>
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

        var activityType = activity.GetType();
        var compensateType = activityType.IsGenericType
            && activityType.GetGenericTypeDefinition() == typeof(CatchFaultActivity<,>)
                ? activityType.GetGenericArguments().Skip(1).First()
                : null;

        if (compensateType != null)
        {
            AddCurrentEdge();

            var previousEvent = CurrentEvent;

            var eventType = typeof(MessageEvent<>).MakeGenericType(compensateType);
            var evt = (Event)(Activator.CreateInstance(eventType, compensateType.Name) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
            _currentEvent = GetEventVertex(evt);

            _edges.Add(new Edge(previousEvent, CurrentEvent, CurrentEvent.Title));

            next(activity);

            _currentEvent = previousEvent;
            return;
        }

        next(activity);
    }

    void AddCurrentEdge()
    {
        if (CurrentEvent.IsComposite || _currentEdge != null)
            return;

        _currentEdge = new Edge(CurrentState, CurrentEvent, CurrentEvent.Title);
        _edges.Add(_currentEdge);
    }

    void InspectTransitionActivity(TransitionActivity<TSaga> transitionActivity)
    {
        AddCurrentEdge();

        var targetState = GetStateVertex(transitionActivity.ToState);

        _edges.Add(new Edge(CurrentEvent, targetState, CurrentEvent.Title));
    }

    void InspectCompositeEventActivity(CompositeEventActivity<TSaga> compositeActivity)
    {
        AddCurrentEdge();

        var compositeEvent = GetEventVertex(compositeActivity.Event);

        _edges.Add(new Edge(CurrentEvent, compositeEvent, compositeEvent.Title));
    }

    Vertex GetStateVertex(State state)
    {
        if (_states.TryGetValue(state, out var vertex))
            return vertex;

        vertex = CreateStateVertex(state);
        _states.Add(state, vertex);

        return vertex;
    }

    Vertex GetEventVertex(Event state)
    {
        if (_events.TryGetValue(state, out var vertex))
            return vertex;

        vertex = CreateEventVertex(state);
        _events.Add(state, vertex);

        return vertex;
    }

    Vertex CurrentEvent => _currentEvent
        ?? throw new InvalidOperationException("A state-machine event must be visited before its activities.");

    Vertex CurrentState => _currentState
        ?? throw new InvalidOperationException("A state-machine state must be visited before its events.");

    static Vertex CreateStateVertex(State state)
    {
        return new Vertex(typeof(State), typeof(State), state.Name, false);
    }

    Vertex CreateEventVertex(Event @event)
    {
        var targetType = @event
            .GetType()
            .GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == typeof(Event<>))
            .Select(x => x.GetGenericArguments()[0])
            .DefaultIfEmpty(typeof(Event))
            .Single();

        return new Vertex(typeof(Event), targetType, @event.Name, _machine.IsCompositeEvent(@event));
    }

    static Vertex CreateEventVertex(Type exceptionType)
    {
        return new Vertex(typeof(Event), exceptionType, exceptionType.Name, false);
    }
}
