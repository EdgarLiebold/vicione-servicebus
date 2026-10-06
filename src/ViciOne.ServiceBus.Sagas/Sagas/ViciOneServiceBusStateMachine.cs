using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Declares saga states, event correlation, request/response handling and scheduled-message handling.
/// </summary>
/// <typeparam name="TInstance">The saga instance type whose current state is managed.</typeparam>
public partial class ViciOneServiceBusStateMachine<TInstance> :
    ISagaStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly HashSet<string> _compositeEvents;
    readonly Dictionary<string, StateMachineEvent> _eventCache;
    readonly Dictionary<IEvent, IEventCorrelation> _eventCorrelations;
    readonly EventObservable _eventObservers;
    readonly IState<TInstance> _final;
    readonly IState<TInstance> _initial;
    readonly Lazy<IStateMachineRegistration[]> _registrations;
    readonly Dictionary<string, IState<TInstance>> _stateCache;
    readonly StateObservable _stateObservers;
    IStateAccessor<TInstance> _accessor;

    List<FieldInfo> _backingFields = null!;
    Func<IBehaviorContext<TInstance>, Task<bool>> _isCompleted;
    string _name;
    List<PropertyInfo> _stateMachineProperties = null!;
    UnhandledEventCallback<TInstance> _unhandledEventCallback;

    /// <summary>Creates the boundary states and default accessor, then initializes discoverable state and event properties.</summary>
    protected ViciOneServiceBusStateMachine()
    {
        _registrations = new Lazy<IStateMachineRegistration[]>(() => GetRegistrations());
        _stateCache = new Dictionary<string, IState<TInstance>>(16);
        _eventCache = new Dictionary<string, StateMachineEvent>(16);
        _compositeEvents = new HashSet<string>();

        _eventObservers = new EventObservable();
        _stateObservers = new StateObservable();

        _initial = new StateMachineState((context, state) => UnhandledEventAsync(context, state), "Initial", _eventObservers);
        _stateCache[_initial.Name] = _initial;
        _final = new StateMachineState((context, state) => UnhandledEventAsync(context, state), "Final", _eventObservers);
        _stateCache[_final.Name] = _final;

        _accessor = new DefaultInstanceStateAccessor(this, _stateCache[Initial.Name], _stateObservers);

        _unhandledEventCallback = DefaultUnhandledEventCallbackAsync;

        _name = GetType().Name;

        _eventCorrelations = new Dictionary<IEvent, IEventCorrelation>();
        _isCompleted = NotCompletedByDefaultAsync;

        RegisterImplicit();
    }

    IEnumerable<IState<TInstance>> IntrospectionStates
    {
        get
        {
            yield return _initial;

            foreach (IState<TInstance> x in _stateCache.Values)
            {
                if (Equals(x, Initial) || Equals(x, Final))
                    continue;

                yield return x;
            }

            yield return _final;
        }
    }

    /// <summary>Enumerates configured correlations for the currently declared non-transition events.</summary>
    public IEnumerable<IEventCorrelation> Correlations
    {
        get
        {
            foreach (var @event in Events)
            {
                if (_eventCorrelations.TryGetValue(@event, out var correlation))
                    yield return correlation;
            }
        }
    }

    async Task<bool> ISagaStateMachine<TInstance>.IsCompletedAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return await _isCompleted(context).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    string IStateMachine.Name => _name;
    /// <summary>Gets the accessor used to read and change the saga instance's current state.</summary>
    public IStateAccessor<TInstance> Accessor => _accessor;
    /// <summary>Gets the initial boundary state used when an instance has no current state.</summary>
    public IState Initial => _initial;
    /// <summary>Gets the final boundary state.</summary>
    public IState Final => _final;

    IState IStateMachine.GetState(string name)
    {
        if (_stateCache.TryGetValue(name, out IState<TInstance>? result))
            return result;

        throw new UnknownStateException(_name, name);
    }

    async Task IStateMachine<TInstance>.RaiseEventAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        IState<TInstance> state = await _accessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new SagaStateMachineException($"The state machine '{_name}' did not initialize its current state.");

        if (!_stateCache.TryGetValue(state.Name, out IState<TInstance>? instanceState))
            throw new UnknownStateException(_name, state.Name);

        await instanceState.RaiseAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    async Task IStateMachine<TInstance>.RaiseEventAsync<T>(IBehaviorContext<TInstance, T> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        IState<TInstance> state = await _accessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new SagaStateMachineException($"The state machine '{_name}' did not initialize its current state.");

        if (!_stateCache.TryGetValue(state.Name, out IState<TInstance>? instanceState))
            throw new UnknownStateException(_name, state.Name);

        await instanceState.RaiseAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Looks up a declared state by its name.</summary>
    /// <param name="name">The declared state name.</param>
    /// <returns>The state registered under the supplied name.</returns>
    /// <exception cref="UnknownStateException">No state is registered under the supplied name.</exception>
    public IState<TInstance> GetState(string name)
    {
        if (TryGetState(name, out IState<TInstance>? result))
            return result;

        throw new UnknownStateException(_name, name);
    }

    /// <summary>Enumerates the currently registered states, including the initial and final states.</summary>
    public IEnumerable<IState> States => _stateCache.Values;

    IEvent IStateMachine.GetEvent(string name)
    {
        if (_eventCache.TryGetValue(name, out var result))
            return result.Event;

        throw new UnknownEventException(_name, name);
    }

    /// <summary>Enumerates the currently declared events, excluding state-transition events.</summary>
    public IEnumerable<IEvent> Events
    {
        get { return _eventCache.Values.Where(x => false == x.IsTransitionEvent).Select(x => x.Event); }
    }

    Type IStateMachine.InstanceType => typeof(TInstance);

    /// <summary>Enumerates the events exposed by the registered state with the supplied state's name.</summary>
    /// <param name="state">The state whose name selects the registered state.</param>
    /// <returns>The selected state's event enumeration.</returns>
    /// <exception cref="UnknownStateException">No state is registered under the supplied state's name.</exception>
    public IEnumerable<IEvent> NextEvents(IState state)
    {
        if (_stateCache.TryGetValue(state.Name, out IState<TInstance>? result))
            return result.Events;

        throw new UnknownStateException(_name, state.Name);
    }

    /// <summary>Checks whether the event's name is registered as a composite event.</summary>
    /// <param name="event">The event whose name is checked.</param>
    /// <returns><see langword="true" /> if the name identifies a composite event; otherwise, <see langword="false" />.</returns>
    public bool IsCompositeEvent(IEvent @event)
    {
        return _compositeEvents.Contains(@event.Name);
    }

    /// <summary>Visits the initial state, registered intermediate states and final state in that order.</summary>
    /// <param name="visitor">The visitor passed to each state.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        foreach (IState<TInstance> x in IntrospectionStates)
            x.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The parent probe context for the machine, its accessor and its states.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("stateMachine");

        var stateMachineType = GetType();
        scope.Add("name", stateMachineType.Name);
        scope.Add("instanceType", TypeCache<TInstance>.ShortName);

        _accessor.Probe(scope);

        foreach (IState<TInstance> state in IntrospectionStates)
            state.Probe(scope);
    }

    /// <summary>Connects an observer for non-transition event notifications.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public IDisposable ConnectEventObserver(IEventObserver<TInstance> observer)
    {
        var eventObserver = new NonTransitionEventObserver<TInstance>(_eventCache, observer);

        return _eventObservers.Connect(eventObserver);
    }

    /// <summary>Connects an observer whose notifications are restricted to the selected event.</summary>
    /// <param name="event">The event used to select matching notifications.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public IDisposable ConnectEventObserver(IEvent @event, IEventObserver<TInstance> observer)
    {
        var eventObserver = new SelectedEventObserver(@event, observer);

        return _eventObservers.Connect(eventObserver);
    }

    /// <summary>Connects an observer for current-state changes performed by the configured accessor.</summary>
    /// <param name="stateObserver">The observer that receives state-change notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public IDisposable ConnectStateObserver(IStateObserver<TInstance> stateObserver)
    {
        return _stateObservers.Connect(stateObserver);
    }

    bool TryGetState(string name, [NotNullWhen(true)] out IState<TInstance>? state)
    {
        return _stateCache.TryGetValue(name, out state);
    }

    Task DefaultUnhandledEventCallbackAsync(IUnhandledEventContext<TInstance> context)
    {
        throw new UnhandledEventException(_name, context.Event.Name, context.CurrentState.Name);
    }

    /// <summary>Stores the current state in the selected state-valued saga property.</summary>
    /// <param name="instanceStateProperty">The saga property used to read and write the current state.</param>
    /// <remarks>
    /// Replaces the previous accessor. An absent current state is initialized to the initial state.
    /// </remarks>
    protected internal void InstanceState(Expression<Func<TInstance, IState?>> instanceStateProperty)
    {
        var stateAccessor = new RawStateAccessor(this, instanceStateProperty, _stateObservers);

        _accessor = new InitialIfNullStateAccessor(_stateCache[Initial.Name], stateAccessor);
    }

    /// <summary>Stores the current state's name in the selected string-valued saga property.</summary>
    /// <param name="instanceStateProperty">The saga property used to read and write the state name.</param>
    /// <remarks>Replaces the previous accessor and initializes an absent current state to the initial state.</remarks>
    protected internal void InstanceState(Expression<Func<TInstance, string>> instanceStateProperty)
    {
        var stateAccessor = new StringStateAccessor(this, instanceStateProperty, _stateObservers);

        _accessor = new InitialIfNullStateAccessor(_stateCache[Initial.Name], stateAccessor);
    }

    /// <summary>Stores the current state as an integer index in the selected saga property.</summary>
    /// <param name="instanceStateProperty">The saga property used to read and write the state index.</param>
    /// <param name="states">The ordered states assigned indexes after the initial and final states.</param>
    /// <remarks>Replaces the previous accessor. Zero means no state; one and two identify the initial and final states.</remarks>
    protected internal void InstanceState(Expression<Func<TInstance, int>> instanceStateProperty, params IState[] states)
    {
        var stateIndex = new StateAccessorIndex(this, _initial, _final, states);

        var stateAccessor = new IntStateAccessor(instanceStateProperty, stateIndex, _stateObservers);

        _accessor = new InitialIfNullStateAccessor(_stateCache[Initial.Name], stateAccessor);
    }

    /// <summary>Specifies the name of the state machine.</summary>
    /// <param name="machineName">A nonempty, non-whitespace machine name.</param>
    protected internal void Name(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
            throw new ArgumentException("The machine name must not be empty", nameof(machineName));

        _name = machineName;
    }

    /// <summary>Declares a trigger event named after the selected property and assigns it to that property.</summary>
    /// <param name="propertyExpression">The trigger-event property on this machine.</param>
    protected internal void Event(Expression<Func<IEvent>> propertyExpression)
    {
        DeclarePropertyBasedEvent(prop => DeclareTriggerEvent(prop.Name), propertyExpression.GetPropertyInfo());
    }

    /// <summary>Creates and registers a trigger event under the supplied name.</summary>
    /// <param name="name">The name used as the event-cache key.</param>
    /// <returns>The newly created trigger event.</returns>
    /// <remarks>A repeated name replaces the cached event.</remarks>
    protected internal IEvent Event(string name)
    {
        return DeclareTriggerEvent(name);
    }

    IEvent DeclareTriggerEvent(string name)
    {
        return DeclareEvent(_ => new TriggerEvent(name), name);
    }

    /// <summary>Sets the asynchronous completion predicate evaluated against the saga instance.</summary>
    /// <param name="completed">The predicate used by the saga repository's completion check.</param>
    protected void SetCompleted(Func<TInstance, Task<bool>> completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        _isCompleted = context => completed(context.Saga);
    }

    /// <summary>Sets the asynchronous completion predicate evaluated against the behavior context.</summary>
    /// <param name="completed">The predicate used by the saga repository's completion check.</param>
    protected void SetCompleted(Func<IBehaviorContext<TInstance>, Task<bool>> completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        _isCompleted = completed;
    }

    /// <summary>Configures the completion check to succeed when the saga instance's current state equals the final state.</summary>
    protected void SetCompletedWhenFinalized()
    {
        _isCompleted = IsFinalizedAsync;
    }

    async Task<bool> IsFinalizedAsync(IBehaviorContext<TInstance> context)
    {
        IState<TInstance>? currentState = await Accessor.GetAsync(context, context.CancellationToken).ConfigureAwait(false);

        return Final.Equals(currentState);
    }

    IEvent<T> DeclareDataEvent<T>(string name)
        where T : class
    {
        return DeclareEvent(_ => new MessageEvent<T>(name), name);
    }

    TEvent DeclarePropertyBasedEvent<TEvent>(Func<PropertyInfo, TEvent> ctor, PropertyInfo property)
        where TEvent : IEvent
    {
        var @event = ctor(property);

        InitializeEvent(this, property, @event);

        return @event;
    }

    TEvent DeclareEvent<TEvent>(Func<string, TEvent> ctor, string name)
        where TEvent : IEvent
    {
        var @event = ctor(name);
        _eventCache[name] = new StateMachineEvent(@event, false);
        return @event;
    }

    /// <summary>
    /// Declares a message event on this machine and configures its correlation from any existing correlation.
    /// </summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="propertyExpression">The message-event property initialized by the declaration.</param>
    /// <param name="configureEventCorrelation">The callback that configures the event's correlation.</param>
    protected void Event<T>(Expression<Func<IEvent<T>>> propertyExpression, Action<IEventCorrelationConfigurator<TInstance, T>> configureEventCorrelation)
        where T : class
    {
        Event(propertyExpression);

        var propertyInfo = propertyExpression.GetPropertyInfo();

        var @event = propertyInfo.GetValue(this) as IEvent<T>
            ?? throw new InvalidOperationException($"The event property '{propertyInfo.Name}' was not initialized.");

        _eventCorrelations.TryGetValue(@event, out var existingCorrelation);

        var configurator = new StateMachineInterfaceType<TInstance, T>.ViciOneServiceBusEventCorrelationConfigurator(this, @event, existingCorrelation);

        configureEventCorrelation(configurator);

        _eventCorrelations[@event] = configurator.Build();
    }

    /// <summary>
    /// Declares a message event on an initialized containing property and configures its correlation.
    /// </summary>
    /// <typeparam name="TProperty">The containing object's type.</typeparam>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="propertyExpression">The initialized containing property on this machine.</param>
    /// <param name="eventPropertyExpression">The message-event property on the containing object.</param>
    /// <param name="configureEventCorrelation">The callback that configures the event's correlation.</param>
    protected internal void Event<TProperty, T>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IEvent<T>>> eventPropertyExpression,
        Action<IEventCorrelationConfigurator<TInstance, T>> configureEventCorrelation)
        where TProperty : class
        where T : class
    {
        Event(propertyExpression, eventPropertyExpression);

        var propertyInfo = propertyExpression.GetPropertyInfo();
        var property = propertyInfo.GetValue(this) as TProperty
            ?? throw new InvalidOperationException($"The containing property '{propertyInfo.Name}' was not initialized.");

        var eventPropertyInfo = eventPropertyExpression.GetPropertyInfo();
        var @event = eventPropertyInfo.GetValue(property) as IEvent<T>
            ?? throw new InvalidOperationException($"The event property '{eventPropertyInfo.Name}' was not initialized.");

        _eventCorrelations.TryGetValue(@event, out var existingCorrelation);

        var configurator = new StateMachineInterfaceType<TInstance, T>.ViciOneServiceBusEventCorrelationConfigurator(this, @event, existingCorrelation);

        configureEventCorrelation(configurator);

        _eventCorrelations[@event] = configurator.Build();
    }

    /// <summary>Creates a message event on an initialized containing property, using both property names as its qualified name.</summary>
    /// <typeparam name="TProperty">The containing object's type.</typeparam>
    /// <typeparam name="T">The event's message type.</typeparam>
    /// <param name="propertyExpression">The initialized containing property on this machine.</param>
    /// <param name="eventPropertyExpression">The message-event property to initialize on the containing object.</param>
    protected internal void Event<TProperty, T>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IEvent<T>>> eventPropertyExpression)
        where TProperty : class
        where T : class
    {
        var property = propertyExpression.GetPropertyInfo();
        var propertyValue = property.GetValue(this, null) as TProperty;
        if (propertyValue == null)
            throw new ArgumentException("The property is not initialized: " + property.Name, nameof(propertyExpression));

        var eventProperty = eventPropertyExpression.GetPropertyInfo();

        var name = $"{property.Name}.{eventProperty.Name}";

        var @event = new MessageEvent<T>(name);

        InitializeEventProperty<TProperty, T>(eventProperty, propertyValue, @event);

        _eventCache[name] = new StateMachineEvent(@event, false);
    }

    /// <summary>
    /// Declares a message event on this machine and registers the message type's correlation convention.
    /// An event without a usable convention retains an uncorrelated registration for configuration validation.
    /// </summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="propertyExpression">The message-event property to initialize.</param>
    protected internal void Event<T>(Expression<Func<IEvent<T>>> propertyExpression)
        where T : class
    {
        DeclarePropertyBasedEvent(prop => DeclareDataEvent<T>(prop.Name), propertyExpression.GetPropertyInfo());

        var propertyInfo = propertyExpression.GetPropertyInfo();

        var @event = propertyInfo.GetValue(this) as IEvent
            ?? throw new InvalidOperationException($"The event property '{propertyInfo.Name}' was not initialized.");

        var registration = GetEventRegistration(@event, typeof(T));

        registration.RegisterCorrelation(this);
    }

    /// <summary>
    /// Creates a named message event and registers the message type's correlation convention.
    /// </summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="name">The name used as the event-cache key.</param>
    /// <returns>The newly created message event.</returns>
    /// <remarks>A repeated name replaces the cached event.</remarks>
    protected internal IEvent<T> Event<T>(string name)
        where T : class
    {
        IEvent<T> @event = DeclareDataEvent<T>(name);

        var registration = GetEventRegistration(@event, typeof(T));

        registration.RegisterCorrelation(this);

        return @event;
    }

    /// <summary>
    /// Creates a named message event, registers its correlation convention and applies the supplied correlation callback.
    /// </summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="name">The name used as the event-cache key.</param>
    /// <param name="configure">The callback that updates the convention-based correlation.</param>
    /// <returns>The newly created message event with its configured correlation.</returns>
    /// <remarks>A repeated name replaces the cached event.</remarks>
    protected internal IEvent<T> Event<T>(string name, Action<IEventCorrelationConfigurator<TInstance, T>> configure)
        where T : class
    {
        IEvent<T> @event = Event<T>(name);

        _eventCorrelations.TryGetValue(@event, out var existingCorrelation);

        var configurator = new StateMachineInterfaceType<TInstance, T>.ViciOneServiceBusEventCorrelationConfigurator(this, @event, existingCorrelation);

        configure?.Invoke(configurator);

        _eventCorrelations[@event] = configurator.Build();

        return @event;
    }

    /// <summary>
    /// Declares a composite event tracked in a status-valued saga property, excluding the initial and final states.
    /// </summary>
    /// <param name="propertyExpression">The trigger-event property initialized for the composite event.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The declared composite event.</returns>
    protected internal IEvent CompositeEvent(Expression<Func<IEvent>> propertyExpression,
        Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression,
        params IEvent[] events)
    {
        return CompositeEvent(propertyExpression, trackingPropertyExpression, CompositeEventOptions.None, events);
    }

    /// <summary>
    /// Declares a composite event tracked in a status-valued saga property with configurable boundary-state inclusion.
    /// </summary>
    /// <param name="propertyExpression">The trigger-event property initialized for the composite event.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="options">The options controlling the composite activity and inclusion of the initial and final states.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The declared composite event.</returns>
    protected internal IEvent CompositeEvent(Expression<Func<IEvent>> propertyExpression,
        Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression,
        CompositeEventOptions options, params IEvent[] events)
    {
        var trackingPropertyInfo = trackingPropertyExpression.GetPropertyInfo();

        var accessor = new StructCompositeEventStatusAccessor<TInstance>(trackingPropertyInfo);

        return CompositeEvent(propertyExpression, accessor, options, events);
    }

    /// <summary>
    /// Declares a composite event tracked in an integer-valued saga property, excluding the initial and final states.
    /// </summary>
    /// <param name="propertyExpression">The trigger-event property initialized for the composite event.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The declared composite event.</returns>
    protected internal IEvent CompositeEvent(Expression<Func<IEvent>> propertyExpression, Expression<Func<TInstance, int>> trackingPropertyExpression,
        params IEvent[] events)
    {
        return CompositeEvent(propertyExpression, trackingPropertyExpression, CompositeEventOptions.None, events);
    }

    /// <summary>
    /// Declares a composite event tracked in an integer-valued saga property with configurable boundary-state inclusion.
    /// </summary>
    /// <param name="propertyExpression">The trigger-event property initialized for the composite event.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="options">The options controlling the composite activity and inclusion of the initial and final states.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The declared composite event.</returns>
    protected internal IEvent CompositeEvent(Expression<Func<IEvent>> propertyExpression, Expression<Func<TInstance, int>> trackingPropertyExpression,
        CompositeEventOptions options, params IEvent[] events)
    {
        var trackingPropertyInfo = trackingPropertyExpression.GetPropertyInfo();

        var accessor = new IntCompositeEventStatusAccessor<TInstance>(trackingPropertyInfo);

        return CompositeEvent(propertyExpression, accessor, options, events);
    }

    internal IEvent CompositeEvent(string name, Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression, params IEvent[] events)
    {
        return CompositeEvent(name, trackingPropertyExpression, CompositeEventOptions.None, events);
    }

    /// <summary>Creates a named composite event tracked in a status-valued saga property.</summary>
    /// <param name="name">The name used to register the new composite event.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="options">The options controlling the activity and boundary-state inclusion.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The newly declared composite event.</returns>
    protected internal IEvent CompositeEvent(string name, Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression,
        CompositeEventOptions options,
        params IEvent[] events)
    {
        return CompositeEvent(name, new StructCompositeEventStatusAccessor<TInstance>(trackingPropertyExpression.GetPropertyInfo()), options, events);
    }

    internal IEvent CompositeEvent(string name, Expression<Func<TInstance, int>> trackingPropertyExpression, params IEvent[] events)
    {
        return CompositeEvent(name, trackingPropertyExpression, CompositeEventOptions.None, events);
    }

    /// <summary>Creates a named composite event tracked in an integer-valued saga property.</summary>
    /// <param name="name">The name used to register the new composite event.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="options">The options controlling the activity and boundary-state inclusion.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The newly declared composite event.</returns>
    protected internal IEvent CompositeEvent(string name, Expression<Func<TInstance, int>> trackingPropertyExpression, CompositeEventOptions options,
        params IEvent[] events)
    {
        return CompositeEvent(name, new IntCompositeEventStatusAccessor<TInstance>(trackingPropertyExpression.GetPropertyInfo()), options, events);
    }

    /// <summary>Attaches status-backed composite tracking to an existing event, excluding the initial and final states.</summary>
    /// <param name="event">The event raised when all required-event flags are set.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The supplied composite event.</returns>
    protected internal IEvent CompositeEvent(IEvent @event, Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression,
        params IEvent[] events)
    {
        return CompositeEvent(@event, trackingPropertyExpression, CompositeEventOptions.None, events);
    }

    /// <summary>Attaches status-backed composite tracking to an existing event with configurable boundary-state inclusion.</summary>
    /// <param name="event">The event raised when all required-event flags are set.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="options">The options controlling the activity and boundary-state inclusion.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The supplied composite event.</returns>
    protected internal IEvent CompositeEvent(IEvent @event,
        Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression,
        CompositeEventOptions options,
        params IEvent[] events)
    {
        return CompositeEvent(@event, new StructCompositeEventStatusAccessor<TInstance>(trackingPropertyExpression.GetPropertyInfo()), options, events);
    }

    /// <summary>Attaches integer-backed composite tracking to an existing event, excluding the initial and final states.</summary>
    /// <param name="event">The event raised when all required-event flags are set.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The supplied composite event.</returns>
    protected internal IEvent CompositeEvent(IEvent @event,
        Expression<Func<TInstance, int>> trackingPropertyExpression,
        params IEvent[] events)
    {
        return CompositeEvent(@event, trackingPropertyExpression, CompositeEventOptions.None, events);
    }

    /// <summary>Attaches integer-backed composite tracking to an existing event with configurable boundary-state inclusion.</summary>
    /// <param name="event">The event raised when all required-event flags are set.</param>
    /// <param name="trackingPropertyExpression">The saga property that stores the required-event flags.</param>
    /// <param name="options">The options controlling the activity and boundary-state inclusion.</param>
    /// <param name="events">The one to 31 events whose flags together complete the composite event.</param>
    /// <returns>The supplied composite event.</returns>
    protected internal IEvent CompositeEvent(IEvent @event,
        Expression<Func<TInstance, int>> trackingPropertyExpression,
        CompositeEventOptions options,
        params IEvent[] events)
    {
        return CompositeEvent(@event, new IntCompositeEventStatusAccessor<TInstance>(trackingPropertyExpression.GetPropertyInfo()),
            options, events);
    }

    IEvent CompositeEvent(Expression<Func<IEvent>> propertyExpression, ICompositeEventStatusAccessor<TInstance> accessor,
        CompositeEventOptions options, IEvent[] events)
    {
        ValidateCompositeEvents(events);

        IEvent CreateEvent()
        {
            var eventProperty = propertyExpression.GetPropertyInfo();

            var @event = new TriggerEvent(eventProperty.Name);

            InitializeEvent(this, eventProperty, @event);

            _eventCache[eventProperty.Name] = new StateMachineEvent(@event, false);

            return @event;
        }

        return CompositeEvent(CreateEvent(), accessor, options, events);
    }

    IEvent CompositeEvent(string name, ICompositeEventStatusAccessor<TInstance> accessor, CompositeEventOptions options, IEvent[] events)
    {
        ValidateCompositeEvents(events);

        IEvent CreateEvent()
        {
            var @event = new TriggerEvent(name);

            _eventCache[name] = new StateMachineEvent(@event, false);

            return @event;
        }

        return CompositeEvent(CreateEvent(), accessor, options, events);
    }

    IEvent CompositeEvent(IEvent @event, ICompositeEventStatusAccessor<TInstance> accessor, CompositeEventOptions options, IEvent[] events)
    {
        ValidateCompositeEvents(events);

        var complete = new CompositeEventStatus(Enumerable.Range(0, events.Length).Aggregate(0, (current, x) => current | (1 << x)));

        _compositeEvents.Add(@event.Name);

        for (var i = 0; i < events.Length; i++)
        {
            var flag = 1 << i;

            var activity = new CompositeEventActivity<TInstance>(accessor, flag, complete, @event, options);

            bool Filter(IState<TInstance> state)
            {
                if (Equals(state, Initial))
                    return options.HasFlag(CompositeEventOptions.IncludeInitial);

                if (Equals(state, Final))
                    return options.HasFlag(CompositeEventOptions.IncludeFinal);

                return true;
            }

            List<IState<TInstance>> states = _stateCache.Values.Where(Filter).ToList();

            foreach (IState<TInstance> state in states)
            {
                During(state,
                    When(events[i])
                        .Execute(activity));
            }
        }

        return @event;
    }

    static void ValidateCompositeEvents(IEvent[] events)
    {
        if (events == null)
            throw new ArgumentNullException(nameof(events));
        if (events.Length > 31)
            throw new ArgumentException("No more than 31 events can be combined into a single event");
        if (events.Length == 0)
            throw new ArgumentException("At least one event must be specified for a composite event");
        if (events.Any(x => x == null))
            throw new ArgumentException("One or more events specified has not yet been initialized");
    }

    /// <summary>Declares a state named after the selected property and initializes that property.</summary>
    /// <param name="propertyExpression">The state property on this machine.</param>
    protected internal void State(Expression<Func<IState>> propertyExpression)
    {
        var property = propertyExpression.GetPropertyInfo();

        DeclareState(property);
    }

    /// <summary>Returns the registered state with the supplied name, or creates and registers that state.</summary>
    /// <param name="name">The name used to look up or register the state.</param>
    /// <returns>The existing or newly declared state.</returns>
    protected internal IState<TInstance> State(string name)
    {
        if (TryGetState(name, out IState<TInstance>? foundState))
            return foundState;

        var state = new StateMachineState((c, s) => UnhandledEventAsync(c, s), name, _eventObservers);
        SetState(name, state);

        return state;
    }

    void DeclareState(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        var name = property.Name;

        var propertyValue = property.GetValue(this);

        if (TryGetState(name, out IState<TInstance>? registeredState) &&
            registeredState is StateMachineState existingState && existingState.SuperState == null)
        {
            if (!ReferenceEquals(propertyValue, existingState))
                InitializeState(this, property, existingState);
            return;
        }

        var state = new StateMachineState((c, s) => UnhandledEventAsync(c, s), name, _eventObservers);

        InitializeState(this, property, state);

        SetState(name, state);
    }

    /// <summary>Declares a state on an initialized containing object, using both property names as its qualified name.</summary>
    /// <typeparam name="TProperty">The containing object's type.</typeparam>
    /// <param name="propertyExpression">The initialized containing property on this machine.</param>
    /// <param name="statePropertyExpression">The state property to initialize on the containing object.</param>
    protected internal void State<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression)
        where TProperty : class
    {
        var property = propertyExpression.GetPropertyInfo();
        var propertyValue = property.GetValue(this, null) as TProperty;
        if (propertyValue == null)
            throw new ArgumentException("The property is not initialized: " + property.Name, nameof(propertyExpression));

        var stateProperty = statePropertyExpression.GetPropertyInfo();

        var name = $"{property.Name}.{stateProperty.Name}";

        var propertyState = GetStateProperty(stateProperty, propertyValue);
        if (TryGetState(name, out IState<TInstance>? registeredState) &&
            registeredState is StateMachineState existingState && existingState.SuperState == null)
        {
            if (!ReferenceEquals(propertyState, existingState))
                InitializeStateProperty(stateProperty, propertyValue, existingState);
            return;
        }

        var state = new StateMachineState((c, s) => UnhandledEventAsync(c, s), name, _eventObservers);

        InitializeStateProperty(stateProperty, propertyValue, state);

        SetState(name, state);
    }

    static StateMachineState? GetStateProperty<TProperty>(PropertyInfo stateProperty, TProperty propertyValue)
        where TProperty : class
    {
        if (stateProperty.CanRead)
            return stateProperty.GetValue(propertyValue) as StateMachineState;

        var objectProperty = propertyValue.GetType().GetProperty(stateProperty.Name, typeof(IState));
        if (objectProperty == null || !objectProperty.CanRead)
            throw new ArgumentException($"The state property is not readable: {stateProperty.Name}");

        return objectProperty.GetValue(propertyValue) as StateMachineState;
    }

    /// <summary>
    /// Declares a state property whose parent is the registered state with the supplied superstate's name.
    /// </summary>
    /// <param name="propertyExpression">The substate property on this machine.</param>
    /// <param name="superState">The state whose name selects the registered parent.</param>
    protected internal void SubState(Expression<Func<IState>> propertyExpression, IState superState)
    {
        if (superState == null)
            throw new ArgumentNullException(nameof(superState));

        IState<TInstance> superStateInstance = GetState(superState.Name);

        var property = propertyExpression.GetPropertyInfo();

        var name = property.Name;

        ValidateSubstateParent(name, superStateInstance);

        var propertyValue = property.GetValue(this);

        if (TryGetState(name, out IState<TInstance>? registeredState) &&
            registeredState is StateMachineState existingState &&
            ReferenceEquals(existingState.SuperState, superStateInstance))
        {
            if (!ReferenceEquals(propertyValue, existingState))
                InitializeState(this, property, existingState);
            return;
        }

        var state = new StateMachineState((c, s) => UnhandledEventAsync(c, s), name, _eventObservers, superStateInstance);

        InitializeState(this, property, state);

        SetState(name, state);
    }

    /// <summary>Returns a matching named substate, or registers a new substate under the selected parent.</summary>
    /// <param name="name">The name used to look up or register the substate.</param>
    /// <param name="superState">The state whose name selects the registered parent.</param>
    /// <returns>The existing substate with the same parent name, or the newly declared substate.</returns>
    protected internal IState<TInstance> SubState(string name, IState superState)
    {
        if (superState == null)
            throw new ArgumentNullException(nameof(superState));

        IState<TInstance> superStateInstance = GetState(superState.Name);

        ValidateSubstateParent(name, superStateInstance);

        // The registered parent instance must match, including after a same-name parent is replaced.
        if (TryGetState(name, out IState<TInstance>? existingState) &&
            name.Equals(existingState?.Name) &&
            ReferenceEquals(existingState?.SuperState, superStateInstance))
            return existingState;

        var state = new StateMachineState((c, s) => UnhandledEventAsync(c, s), name, _eventObservers, superStateInstance);

        SetState(name, state);
        return state;
    }

    /// <summary>Declares a substate on an initialized containing object under the selected registered parent.</summary>
    /// <typeparam name="TProperty">The containing object's type.</typeparam>
    /// <param name="propertyExpression">The initialized containing property on this machine.</param>
    /// <param name="statePropertyExpression">The substate property to initialize on the containing object.</param>
    /// <param name="superState">The state whose name selects the registered parent.</param>
    protected internal void SubState<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression, IState superState)
        where TProperty : class
    {
        if (superState == null)
            throw new ArgumentNullException(nameof(superState));

        IState<TInstance> superStateInstance = GetState(superState.Name);

        var property = propertyExpression.GetPropertyInfo();
        var propertyValue = property.GetValue(this, null) as TProperty;
        if (propertyValue == null)
            throw new ArgumentException("The property is not initialized: " + property.Name, nameof(propertyExpression));

        var stateProperty = statePropertyExpression.GetPropertyInfo();

        var name = $"{property.Name}.{stateProperty.Name}";

        ValidateSubstateParent(name, superStateInstance);

        var propertyState = GetStateProperty(stateProperty, propertyValue);
        if (TryGetState(name, out IState<TInstance>? registeredState) &&
            registeredState is StateMachineState existingState &&
            ReferenceEquals(existingState.SuperState, superStateInstance))
        {
            if (!ReferenceEquals(propertyState, existingState))
                InitializeStateProperty(stateProperty, propertyValue, existingState);
            return;
        }

        var state = new StateMachineState((c, s) => UnhandledEventAsync(c, s), name, _eventObservers, superStateInstance);

        InitializeStateProperty(stateProperty, propertyValue, state);

        SetState(name, state);
    }

    void ValidateSubstateParent(string name, IState<TInstance> superState)
    {
        if (TryGetState(name, out IState<TInstance>? registeredState) &&
            registeredState is StateMachineState existingState && existingState.HasState(superState))
            throw new ArgumentException("A state cannot be a substate of itself or one of its descendants", nameof(superState));
    }

    /// <summary>Adds the state, and state transition events, to the cache.</summary>
    /// <param name="name">The state-cache key.</param>
    /// <param name="state">The state whose four transition events are registered.</param>
    void SetState(string name, StateMachineState state)
    {
        if (_stateCache.TryGetValue(name, out IState<TInstance>? previous) &&
            !ReferenceEquals(previous, state) && previous is StateMachineState previousState)
        {
            (previousState.SuperState as StateMachineState)?.RemoveSubstate(previousState);
            (state.SuperState as StateMachineState)?.AddSubstate(state);
            previousState.MoveSubstatesTo(state);
        }

        _stateCache[name] = state;

        _eventCache[state.BeforeEnter.Name] = new StateMachineEvent(state.BeforeEnter, true);
        _eventCache[state.Enter.Name] = new StateMachineEvent(state.Enter, true);
        _eventCache[state.Leave.Name] = new StateMachineEvent(state.Leave, true);
        _eventCache[state.AfterLeave.Name] = new StateMachineEvent(state.AfterLeave, true);
    }

    /// <summary>Declares the events and associated activities that are handled during the specified state.</summary>
    /// <param name="state">The state whose name selects the registered state.</param>
    /// <param name="activities">The event bindings whose activities are attached to the registered state.</param>
    protected internal void During(IState state, params IEventActivities<TInstance>[] activities)
    {
        IActivityBinder<TInstance>[] activitiesBinder = activities.SelectMany(x => x.GetStateActivityBinders()).ToArray();

        BindActivitiesToState(state, activitiesBinder);
    }

    /// <summary>Declares the events and associated activities that are handled during the specified states.</summary>
    /// <param name="state1">The first state selected by its registered name.</param>
    /// <param name="state2">The second state selected by its registered name.</param>
    /// <param name="activities">The event bindings whose activities are attached to both states.</param>
    protected internal void During(IState state1, IState state2, params IEventActivities<TInstance>[] activities)
    {
        IActivityBinder<TInstance>[] activitiesBinder = activities.SelectMany(x => x.GetStateActivityBinders()).ToArray();

        BindActivitiesToState(state1, activitiesBinder);
        BindActivitiesToState(state2, activitiesBinder);
    }

    /// <summary>Declares the events and associated activities that are handled during the specified states.</summary>
    /// <param name="state1">The first state selected by its registered name.</param>
    /// <param name="state2">The second state selected by its registered name.</param>
    /// <param name="state3">The third state selected by its registered name.</param>
    /// <param name="activities">The event bindings whose activities are attached to all three states.</param>
    protected internal void During(IState state1, IState state2, IState state3, params IEventActivities<TInstance>[] activities)
    {
        IActivityBinder<TInstance>[] activitiesBinder = activities.SelectMany(x => x.GetStateActivityBinders()).ToArray();

        BindActivitiesToState(state1, activitiesBinder);
        BindActivitiesToState(state2, activitiesBinder);
        BindActivitiesToState(state3, activitiesBinder);
    }

    /// <summary>Declares the events and associated activities that are handled during the specified states.</summary>
    /// <param name="state1">The first state selected by its registered name.</param>
    /// <param name="state2">The second state selected by its registered name.</param>
    /// <param name="state3">The third state selected by its registered name.</param>
    /// <param name="state4">The fourth state selected by its registered name.</param>
    /// <param name="activities">The event bindings whose activities are attached to all four states.</param>
    protected internal void During(IState state1, IState state2, IState state3, IState state4,
        params IEventActivities<TInstance>[] activities)
    {
        IActivityBinder<TInstance>[] activitiesBinder = activities.SelectMany(x => x.GetStateActivityBinders()).ToArray();

        BindActivitiesToState(state1, activitiesBinder);
        BindActivitiesToState(state2, activitiesBinder);
        BindActivitiesToState(state3, activitiesBinder);
        BindActivitiesToState(state4, activitiesBinder);
    }

    /// <summary>Declares the events and associated activities that are handled during the specified states.</summary>
    /// <param name="states">The states selected individually by their registered names.</param>
    /// <param name="activities">The event bindings whose activities are attached to every selected state.</param>
    protected internal void During(IEnumerable<IState> states, params IEventActivities<TInstance>[] activities)
    {
        IActivityBinder<TInstance>[] activitiesBinder = activities.SelectMany(x => x.GetStateActivityBinders()).ToArray();

        foreach (var state in states)
            BindActivitiesToState(state, activitiesBinder);
    }

    void BindActivitiesToState(IState state, IActivityBinder<TInstance>[] eventActivities)
    {
        IState<TInstance> activityState = GetState(state.Name);

        foreach (IActivityBinder<TInstance> activity in eventActivities)
            activity.Bind(activityState);
    }

    /// <summary>Declares the events and activities that are handled during the initial state.</summary>
    /// <param name="activities">The event bindings whose activities are attached to the initial state.</param>
    protected internal void Initially(params IEventActivities<TInstance>[] activities)
    {
        During(Initial, activities);
    }

    /// <summary>Binds ordinary events to intermediate states and composite or matching transition events to boundary states.</summary>
    /// <param name="activities">The event bindings distributed across the currently registered states.</param>
    protected internal void DuringAny(params IEventActivities<TInstance>[] activities)
    {
        IActivityBinder<TInstance>[] activitiesBinder = activities.SelectMany(x => x.GetStateActivityBinders()).ToArray();

        IEnumerable<IState<TInstance>> states = _stateCache.Values.Where(x => !Equals(x, Initial) && !Equals(x, Final));

        // Ordinary event bindings are attached only to intermediate states.
        foreach (IState<TInstance> state in states)
            BindActivitiesToState(state, activitiesBinder);

        // Composite-event bindings also apply to the initial and final states.
        IActivityBinder<TInstance>[] compositeEvents = activitiesBinder.Where(binder => IsCompositeEvent(binder.Event)).ToArray();
        BindActivitiesToState(_initial, compositeEvents);
        BindActivitiesToState(_final, compositeEvents);

        BindTransitionEvents(_initial, activities);
        BindTransitionEvents(_final, activities);
    }

    /// <summary>Configures final-state entry activities through the machine's any-state binding rules.</summary>
    /// <param name="activityCallback">The callback that supplies activities for the final state's Enter event.</param>
    protected internal void Finally(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        IEventActivityBinder<TInstance> binder = When(Final.Enter);

        binder = activityCallback(binder);

        DuringAny(binder);
    }

    void BindTransitionEvents(IState<TInstance> state, IEnumerable<IEventActivities<TInstance>> activities)
    {
        IEnumerable<IActivityBinder<TInstance>> eventActivities = activities
            .SelectMany(activity => activity.GetStateActivityBinders().Where(x => x.IsStateTransitionEvent(state)));

        foreach (IActivityBinder<TInstance> eventActivity in eventActivities)
            eventActivity.Bind(state);
    }

    /// <summary>Creates an unfiltered activity binder for a trigger event.</summary>
    /// <param name="event">The trigger event whose activities will be configured.</param>
    /// <returns>The binder to configure before attaching it to a state.</returns>
    protected internal IEventActivityBinder<TInstance> When(IEvent @event)
    {
        return When(@event, null);
    }

    /// <summary>Creates a trigger-event activity binder with an optional behavior-context condition.</summary>
    /// <param name="event">The trigger event whose activities will be configured.</param>
    /// <param name="filter">The optional condition applied to the event's behavior context.</param>
    /// <returns>The binder to configure before attaching it to a state.</returns>
    protected internal IEventActivityBinder<TInstance> When(IEvent @event, StateMachineCondition<TInstance>? filter)
    {
        return new TriggerEventActivityBinder<TInstance>(this, @event, filter);
    }

    /// <summary>Configures and binds activities for the selected registered state's Enter event.</summary>
    /// <param name="state">The state whose name selects the registered state.</param>
    /// <param name="activityCallback">The callback that supplies the entry-event activities.</param>
    protected internal void WhenEnter(IState state, Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        IState<TInstance> activityState = GetState(state.Name);

        IEventActivityBinder<TInstance> binder = new TriggerEventActivityBinder<TInstance>(this, activityState.Enter);

        binder = activityCallback(binder);

        During(state, binder);
    }

    /// <summary>Configures Enter-event activities for every currently registered state, including boundary states.</summary>
    /// <param name="activityCallback">The callback invoked to configure each state's entry-event binder.</param>
    protected internal void WhenEnterAny(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        BindEveryTransitionEvent(activityCallback, x => x.Enter);
    }

    /// <summary>Configures Leave-event activities for every currently registered state, including boundary states.</summary>
    /// <param name="activityCallback">The callback invoked to configure each state's exit-event binder.</param>
    protected internal void WhenLeaveAny(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        BindEveryTransitionEvent(activityCallback, x => x.Leave);
    }

    void BindEveryTransitionEvent(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback,
        Func<IState<TInstance>, IEvent> eventProvider)
    {
        IState<TInstance>[] states = _stateCache.Values.ToArray();

        IActivityBinder<TInstance>[] binders = states.Select(state =>
        {
            IEventActivityBinder<TInstance> binder = new TriggerEventActivityBinder<TInstance>(this, eventProvider(state));

            return activityCallback(binder);
        }).SelectMany(x => x.GetStateActivityBinders()).ToArray();

        foreach (IState<TInstance> state in states)
        {
            foreach (IActivityBinder<TInstance> binder in binders)
                binder.Bind(state);
        }
    }

    /// <summary>Configures BeforeEnter activities for every currently registered state, including boundary states.</summary>
    /// <param name="activityCallback">The callback invoked to configure each state's state-valued transition binder.</param>
    protected internal void BeforeEnterAny(Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        BindEveryTransitionEvent(activityCallback, x => x.BeforeEnter);
    }

    /// <summary>Configures AfterLeave activities for every currently registered state, including boundary states.</summary>
    /// <param name="activityCallback">The callback invoked to configure each state's state-valued transition binder.</param>
    protected internal void AfterLeaveAny(Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        BindEveryTransitionEvent(activityCallback, x => x.AfterLeave);
    }

    void BindEveryTransitionEvent(Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback,
        Func<IState<TInstance>, IEvent<IState>> eventProvider)
    {
        IState<TInstance>[] states = _stateCache.Values.ToArray();

        IActivityBinder<TInstance>[] binders = states.Select(state =>
        {
            IEventActivityBinder<TInstance, IState> binder = new DataEventActivityBinder<TInstance, IState>(this, eventProvider(state));

            return activityCallback(binder);
        }).SelectMany(x => x.GetStateActivityBinders()).ToArray();

        foreach (IState<TInstance> state in states)
        {
            foreach (IActivityBinder<TInstance> binder in binders)
                binder.Bind(state);
        }
    }

    /// <summary>Configures and binds activities for the selected registered state's Leave event.</summary>
    /// <param name="state">The state whose name selects the registered state.</param>
    /// <param name="activityCallback">The callback that supplies the exit-event activities.</param>
    protected internal void WhenLeave(IState state, Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        IState<TInstance> activityState = GetState(state.Name);

        IEventActivityBinder<TInstance> binder = new TriggerEventActivityBinder<TInstance>(this, activityState.Leave);

        binder = activityCallback(binder);

        During(state, binder);
    }

    /// <summary>Configures and binds state-valued BeforeEnter activities for the selected registered state.</summary>
    /// <param name="state">The state whose name selects the registered state.</param>
    /// <param name="activityCallback">The callback that supplies the pre-entry transition activities.</param>
    protected internal void BeforeEnter(IState state,
        Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        IState<TInstance> activityState = GetState(state.Name);

        IEventActivityBinder<TInstance, IState> binder = new DataEventActivityBinder<TInstance, IState>(this, activityState.BeforeEnter);

        binder = activityCallback(binder);

        During(state, binder);
    }

    /// <summary>Configures and binds state-valued AfterLeave activities for the selected registered state.</summary>
    /// <param name="state">The state whose name selects the registered state.</param>
    /// <param name="activityCallback">The callback that supplies the post-exit transition activities.</param>
    protected internal void AfterLeave(IState state,
        Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        IState<TInstance> activityState = GetState(state.Name);

        IEventActivityBinder<TInstance, IState> binder = new DataEventActivityBinder<TInstance, IState>(this, activityState.AfterLeave);

        binder = activityCallback(binder);

        During(state, binder);
    }

    /// <summary>Creates an unfiltered activity binder for a message event.</summary>
    /// <typeparam name="TMessage">The event data type.</typeparam>
    /// <param name="event">The message event whose activities will be configured.</param>
    /// <returns>The binder to configure before attaching it to a state.</returns>
    protected internal IEventActivityBinder<TInstance, TMessage> When<TMessage>(IEvent<TMessage> @event)
        where TMessage : class
    {
        return When(@event, null);
    }

    /// <summary>Creates a message-event activity binder with an optional typed behavior-context condition.</summary>
    /// <typeparam name="TMessage">The event data type.</typeparam>
    /// <param name="event">The message event whose activities will be configured.</param>
    /// <param name="filter">The optional condition applied to the event's typed behavior context.</param>
    /// <returns>The binder to configure before attaching it to a state.</returns>
    protected internal IEventActivityBinder<TInstance, TMessage> When<TMessage>(IEvent<TMessage> @event, StateMachineCondition<TInstance, TMessage>? filter)
        where TMessage : class
    {
        return new DataEventActivityBinder<TInstance, TMessage>(this, @event, filter);
    }

    /// <summary>Creates an ignore binding for a trigger event.</summary>
    /// <param name="event">The trigger event selected for ignoring.</param>
    /// <returns>The ignore binding to attach to a state.</returns>
    protected internal IEventActivities<TInstance> Ignore(IEvent @event)
    {
        IActivityBinder<TInstance> activityBinder = new IgnoreEventActivityBinder<TInstance>(@event);

        return new TriggerEventActivityBinder<TInstance>(this, @event, activityBinder);
    }

    /// <summary>Creates an unconditional ignore binding for a message event.</summary>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="event">The message event selected for ignoring.</param>
    /// <returns>The ignore binding to attach to a state.</returns>
    protected internal IEventActivities<TInstance> Ignore<TData>(IEvent<TData> @event)
        where TData : class
    {
        IActivityBinder<TInstance> activityBinder = new IgnoreEventActivityBinder<TInstance>(@event);

        return new DataEventActivityBinder<TInstance, TData>(this, @event, activityBinder);
    }

    /// <summary>Creates a conditional ignore binding for a message event.</summary>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="event">The message event selected for ignoring.</param>
    /// <param name="filter">The condition applied to the typed behavior context.</param>
    /// <returns>The conditional ignore binding to attach to a state.</returns>
    protected internal IEventActivities<TInstance> Ignore<TData>(IEvent<TData> @event, StateMachineCondition<TInstance, TData> filter)
        where TData : class
    {
        IActivityBinder<TInstance> activityBinder = new IgnoreEventActivityBinder<TInstance, TData>(@event, filter);

        return new DataEventActivityBinder<TInstance, TData>(this, @event, activityBinder);
    }

    /// <summary>Specifies a callback to invoke when an event is raised in a state where the event is not handled.</summary>
    /// <param name="callback">The unhandled event callback.</param>
    protected internal void OnUnhandledEvent(UnhandledEventCallback<TInstance> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _unhandledEventCallback = callback;
    }

    Task UnhandledEventAsync(IBehaviorContext<TInstance> context, IState state)
    {
        var unhandledEventContext = new UnhandledEventBehaviorContext(this, context, state);

        return _unhandledEventCallback(unhandledEventContext);
    }

    /// <summary>
    /// Declares a one-response request with saga-property request-ID storage, configured events and a pending state.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="requestIdExpression">The nullable GUID saga property used for request-ID storage and response correlation.</param>
    /// <param name="configureRequest">The optional callback that configures the request before declaration.</param>
    protected void Request<TRequest, TResponse>(Expression<Func<IRequest<TInstance, TRequest, TResponse>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> requestIdExpression,
        Action<IRequestConfigurator<TInstance, TRequest, TResponse>>? configureRequest = default)
        where TRequest : class
        where TResponse : class
    {
        var configurator = new StateMachineRequestConfigurator<TInstance, TRequest, TResponse>();

        configureRequest?.Invoke(configurator);

        Request(propertyExpression, requestIdExpression, configurator.Settings);
    }

    /// <summary>
    /// Declares a one-response request that uses the saga's correlation ID as its request ID, with configured events and a pending state.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="configureRequest">The optional callback that configures the request before declaration.</param>
    protected void Request<TRequest, TResponse>(Expression<Func<IRequest<TInstance, TRequest, TResponse>>> propertyExpression,
        Action<IRequestConfigurator<TInstance, TRequest, TResponse>>? configureRequest = default)
        where TRequest : class
        where TResponse : class
    {
        var configurator = new StateMachineRequestConfigurator<TInstance, TRequest, TResponse>();

        configureRequest?.Invoke(configurator);

        Request(propertyExpression, configurator.Settings);
    }

    /// <summary>
    /// Declares a one-response request with saga-property request-ID storage and settings-controlled event correlations.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="requestIdExpression">The nullable GUID saga property used for request-ID storage and response correlation.</param>
    /// <param name="settings">The request settings and optional completion, fault and timeout correlation callbacks.</param>
    protected void Request<TRequest, TResponse>(Expression<Func<IRequest<TInstance, TRequest, TResponse>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> requestIdExpression, IRequestSettings<TInstance, TRequest, TResponse> settings)
        where TRequest : class
        where TResponse : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var request = new StateMachineRequest<TRequest, TResponse>(property.Name, settings, requestIdExpression);

        InitializeRequest(this, property, request);

        Event(propertyExpression, x => x.Completed, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Completed?.Invoke(x);
        });
        Event(propertyExpression, x => x.Faulted, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Faulted?.Invoke(x);
        });
        Event(propertyExpression, x => x.TimeoutExpired, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.Message.RequestId);
            settings.TimeoutExpired?.Invoke(x);
        });

        State(propertyExpression, x => x.Pending);

        DuringAny(
            When(request.Completed)
                .CancelRequestTimeout(request),
            When(request.Faulted)
                .CancelRequestTimeout(request, false));
    }

    /// <summary>
    /// Declares a one-response request correlated by the saga's correlation ID, with settings-controlled event correlations.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="settings">The request settings and optional completion, fault and timeout correlation callbacks.</param>
    protected internal void Request<TRequest, TResponse>(Expression<Func<IRequest<TInstance, TRequest, TResponse>>> propertyExpression,
        IRequestSettings<TInstance, TRequest, TResponse> settings)
        where TRequest : class
        where TResponse : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var request = new StateMachineRequest<TRequest, TResponse>(property.Name, settings);

        InitializeRequest(this, property, request);

        Event(propertyExpression, x => x.Completed, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Completed?.Invoke(x);
        });
        Event(propertyExpression, x => x.Faulted, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Faulted?.Invoke(x);
        });
        Event(propertyExpression, x => x.TimeoutExpired, x =>
        {
            x.CorrelateById(context => context.Message.RequestId);
            settings.TimeoutExpired?.Invoke(x);
        });

        State(propertyExpression, x => x.Pending);

        DuringAny(
            When(request.Completed)
                .CancelRequestTimeout(request),
            When(request.Faulted)
                .CancelRequestTimeout(request, false));
    }

    /// <summary>
    /// Declares a two-response request with saga-property request-ID storage, configured events and a pending state.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="requestIdExpression">The nullable GUID saga property used for request-ID storage and response correlation.</param>
    /// <param name="configureRequest">The optional callback that configures the request before declaration.</param>
    protected void Request<TRequest, TResponse, TResponse2>(Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> requestIdExpression,
        Action<IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>>? configureRequest = default)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        var configurator = new StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>();

        configureRequest?.Invoke(configurator);

        Request(propertyExpression, requestIdExpression, configurator.Settings);
    }

    /// <summary>
    /// Declares a two-response request that uses the saga's correlation ID as its request ID, with configured events and a pending state.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="configureRequest">The optional callback that configures the request before declaration.</param>
    protected void Request<TRequest, TResponse, TResponse2>(Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2>>> propertyExpression,
        Action<IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>>? configureRequest = default)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        var configurator = new StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2>();

        configureRequest?.Invoke(configurator);

        Request(propertyExpression, configurator.Settings);
    }

    /// <summary>
    /// Declares a two-response request with saga-property request-ID storage and settings-controlled event correlations.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="requestIdExpression">The nullable GUID saga property used for request-ID storage and response correlation.</param>
    /// <param name="settings">The request settings and optional response, fault and timeout correlation callbacks.</param>
    protected internal void Request<TRequest, TResponse, TResponse2>(
        Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> requestIdExpression, IRequestSettings<TInstance, TRequest, TResponse, TResponse2> settings)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var request = new StateMachineRequest<TRequest, TResponse, TResponse2>(property.Name, settings, requestIdExpression);

        InitializeRequest(this, property, request);

        Event(propertyExpression, x => x.Completed, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Completed?.Invoke(x);
        });
        Event(propertyExpression, x => x.Completed2, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Completed2?.Invoke(x);
        });
        Event(propertyExpression, x => x.Faulted, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Faulted?.Invoke(x);
        });
        Event(propertyExpression, x => x.TimeoutExpired, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.Message.RequestId);
            settings.TimeoutExpired?.Invoke(x);
        });

        State(propertyExpression, x => x.Pending);

        DuringAny(
            When(request.Completed)
                .CancelRequestTimeout(request),
            When(request.Completed2)
                .CancelRequestTimeout(request),
            When(request.Faulted)
                .CancelRequestTimeout(request, false));
    }

    /// <summary>
    /// Declares a two-response request correlated by the saga's correlation ID, with settings-controlled event correlations.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="settings">The request settings and optional response, fault and timeout correlation callbacks.</param>
    protected internal void Request<TRequest, TResponse, TResponse2>(
        Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2>>> propertyExpression,
        IRequestSettings<TInstance, TRequest, TResponse, TResponse2> settings)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var request = new StateMachineRequest<TRequest, TResponse, TResponse2>(property.Name, settings);

        InitializeRequest(this, property, request);

        Event(propertyExpression, x => x.Completed, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Completed?.Invoke(x);
        });
        Event(propertyExpression, x => x.Completed2, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Completed2?.Invoke(x);
        });
        Event(propertyExpression, x => x.Faulted, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Faulted?.Invoke(x);
        });
        Event(propertyExpression, x => x.TimeoutExpired, x =>
        {
            x.CorrelateById(context => context.Message.RequestId);
            settings.TimeoutExpired?.Invoke(x);
        });

        State(propertyExpression, x => x.Pending);

        DuringAny(
            When(request.Completed)
                .CancelRequestTimeout(request),
            When(request.Completed2)
                .CancelRequestTimeout(request),
            When(request.Faulted)
                .CancelRequestTimeout(request, false));
    }

    /// <summary>
    /// Declares a three-response request with saga-property request-ID storage, configured events and a pending state.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <typeparam name="TResponse3">The third response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="requestIdExpression">The nullable GUID saga property used for request-ID storage and response correlation.</param>
    /// <param name="configureRequest">The optional callback that configures the request before declaration.</param>
    protected void Request<TRequest, TResponse, TResponse2, TResponse3>(
        Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2, TResponse3>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> requestIdExpression,
        Action<IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3>>? configureRequest = default)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        var configurator = new StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3>();

        configureRequest?.Invoke(configurator);

        Request(propertyExpression, requestIdExpression, configurator.Settings);
    }

    /// <summary>
    /// Declares a three-response request that uses the saga's correlation ID as its request ID, with configured events and a pending state.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <typeparam name="TResponse3">The third response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="configureRequest">The optional callback that configures the request before declaration.</param>
    protected void Request<TRequest, TResponse, TResponse2, TResponse3>(
        Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2, TResponse3>>> propertyExpression,
        Action<IRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3>>? configureRequest = default)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        var configurator = new StateMachineRequestConfigurator<TInstance, TRequest, TResponse, TResponse2, TResponse3>();

        configureRequest?.Invoke(configurator);

        Request(propertyExpression, configurator.Settings);
    }

    /// <summary>
    /// Declares a three-response request with saga-property request-ID storage and settings-controlled event correlations.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <typeparam name="TResponse3">The third response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="requestIdExpression">The nullable GUID saga property used for request-ID storage and response correlation.</param>
    /// <param name="settings">The request settings and optional response, fault and timeout correlation callbacks.</param>
    protected internal void Request<TRequest, TResponse, TResponse2, TResponse3>(
        Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2, TResponse3>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> requestIdExpression, IRequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> settings)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var request = new StateMachineRequest<TRequest, TResponse, TResponse2, TResponse3>(property.Name, settings, requestIdExpression);

        InitializeRequest(this, property, request);

        Event(propertyExpression, x => x.Completed, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Completed?.Invoke(x);
        });
        Event(propertyExpression, x => x.Completed2, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Completed2?.Invoke(x);
        });
        Event(propertyExpression, x => x.Completed3, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Completed3?.Invoke(x);
        });
        Event(propertyExpression, x => x.Faulted, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.RequestId);
            settings.Faulted?.Invoke(x);
        });
        Event(propertyExpression, x => x.TimeoutExpired, x =>
        {
            x.CorrelateBy(requestIdExpression, context => context.Message.RequestId);
            settings.TimeoutExpired?.Invoke(x);
        });

        State(propertyExpression, x => x.Pending);

        DuringAny(
            When(request.Completed)
                .CancelRequestTimeout(request),
            When(request.Completed2)
                .CancelRequestTimeout(request),
            When(request.Completed3)
                .CancelRequestTimeout(request),
            When(request.Faulted)
                .CancelRequestTimeout(request, false));
    }

    /// <summary>
    /// Declares a three-response request correlated by the saga's correlation ID, with settings-controlled event correlations.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The alternate response type.</typeparam>
    /// <typeparam name="TResponse3">The third response type.</typeparam>
    /// <param name="propertyExpression">The request property on the state machine.</param>
    /// <param name="settings">The request settings and optional response, fault and timeout correlation callbacks.</param>
    protected internal void Request<TRequest, TResponse, TResponse2, TResponse3>(
        Expression<Func<IRequest<TInstance, TRequest, TResponse, TResponse2, TResponse3>>> propertyExpression,
        IRequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> settings)
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var request = new StateMachineRequest<TRequest, TResponse, TResponse2, TResponse3>(property.Name, settings);

        InitializeRequest(this, property, request);

        Event(propertyExpression, x => x.Completed, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Completed?.Invoke(x);
        });
        Event(propertyExpression, x => x.Completed2, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Completed2?.Invoke(x);
        });
        Event(propertyExpression, x => x.Completed3, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Completed3?.Invoke(x);
        });
        Event(propertyExpression, x => x.Faulted, x =>
        {
            x.CorrelateById(context => context.RequestId ?? throw new RequestException("Missing RequestId"));
            settings.Faulted?.Invoke(x);
        });
        Event(propertyExpression, x => x.TimeoutExpired, x =>
        {
            x.CorrelateById(context => context.Message.RequestId);
            settings.TimeoutExpired?.Invoke(x);
        });

        State(propertyExpression, x => x.Pending);

        DuringAny(
            When(request.Completed)
                .CancelRequestTimeout(request),
            When(request.Completed2)
                .CancelRequestTimeout(request),
            When(request.Completed3)
                .CancelRequestTimeout(request),
            When(request.Faulted)
                .CancelRequestTimeout(request, false));
    }

    /// <summary>Declares a schedule whose pending message token is stored with the state machine instance.</summary>
    /// <typeparam name="TMessage">The scheduled message type.</typeparam>
    /// <param name="propertyExpression">The schedule property on the state machine.</param>
    /// <param name="tokenIdExpression">The nullable GUID saga property that stores the current scheduled-message token.</param>
    /// <param name="configureSchedule">The optional callback that configures the schedule before declaration.</param>
    protected void Schedule<TMessage>(Expression<Func<ISchedule<TInstance, TMessage>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> tokenIdExpression,
        Action<IScheduleConfigurator<TInstance, TMessage>>? configureSchedule = default)
        where TMessage : class
    {
        var configurator = new StateMachineScheduleConfigurator<TInstance, TMessage>();

        configureSchedule?.Invoke(configurator);

        Schedule(propertyExpression, tokenIdExpression, configurator.Settings);
    }

    /// <summary>Declares a schedule whose pending message token is stored with the state machine instance.</summary>
    /// <typeparam name="TMessage">The scheduled message type.</typeparam>
    /// <param name="propertyExpression">The schedule property on the state machine.</param>
    /// <param name="tokenIdExpression">The nullable GUID saga property that stores the current scheduled-message token.</param>
    /// <param name="settings">The schedule settings and optional scheduled-message correlation callback.</param>
    /// <remarks>The delivery handler rejects mismatched tokens when supplied, requires a current token and clears it after Received only if unchanged.</remarks>
    protected internal void Schedule<TMessage>(Expression<Func<ISchedule<TInstance, TMessage>>> propertyExpression,
        Expression<Func<TInstance, Guid?>> tokenIdExpression,
        IScheduleSettings<TInstance, TMessage> settings)
        where TMessage : class
    {
        var property = propertyExpression.GetPropertyInfo();

        var name = property.Name;

        var schedule = new StateMachineSchedule<TMessage>(name, tokenIdExpression, settings);

        InitializeSchedule(this, property, schedule);

        Event(propertyExpression, x => x.Received);

        if (settings.Received == null)
        {
            Event(propertyExpression, x => x.AnyReceived);

            var registration = GetEventRegistration(schedule.AnyReceived, typeof(TMessage));

            registration.RegisterCorrelation(this);
        }
        else
            Event(propertyExpression, x => x.AnyReceived, x =>
            {
                settings.Received(x);
            });


        DuringAny(
            When(schedule.AnyReceived)
                .ThenAwaited(async context =>
                {
                    Guid? tokenId = schedule.GetTokenId(context.Saga);

                    Guid? messageTokenId = context.GetSchedulingTokenId();
                    if (messageTokenId.HasValue)
                    {
                        if (!tokenId.HasValue || messageTokenId.Value != tokenId.Value)
                        {
                            try
                            {
                                LogContext.Debug?.Log("SAGA: {CorrelationId} Scheduled message not current: {TokenId}", context.Saga.CorrelationId,
                                    messageTokenId.Value);
                            }
                            catch (Exception)
                            {
                                // Diagnostic logging must not change rejection of a stale scheduled message.
                            }

                            return;
                        }
                    }

                    if (!tokenId.HasValue)
                        return;

                    IBehaviorContext<TInstance, TMessage> eventContext = context.CreateProxy(schedule.Received, context.Message);

                    await ((IStateMachine<TInstance>)this).RaiseEventAsync(eventContext, context.CancellationToken).ConfigureAwait(false);

                    if (schedule.GetTokenId(context.Saga) == tokenId)
                        schedule.SetTokenId(context.Saga, default);
                }));
    }

    static Task<bool> NotCompletedByDefaultAsync(IBehaviorContext<TInstance> instance)
    {
        return TaskResults.False;
    }

    void InitializeSchedule<T>(ViciOneServiceBusStateMachine<TInstance> stateMachine, PropertyInfo property, ISchedule<TInstance, T> schedule)
        where T : class
    {
        if (property.CanWrite)
            property.SetValue(stateMachine, schedule);
        else if (TryGetBackingField(property, out var backingField))
            backingField.SetValue(stateMachine, schedule);
        else
            throw new ArgumentException($"The schedule property is not writable: {property.Name}");
    }

    void InitializeRequest<TRequest, TResponse>(ViciOneServiceBusStateMachine<TInstance> stateMachine, PropertyInfo property,
        IRequest<TInstance, TRequest, TResponse> request)
        where TRequest : class
        where TResponse : class
    {
        if (property.CanWrite)
            property.SetValue(stateMachine, request);
        else if (TryGetBackingField(property, out var backingField))
            backingField.SetValue(stateMachine, request);
        else
            throw new ArgumentException($"The request property is not writable: {property.Name}");
    }

    /// <summary>Initializes unset public state and event properties discovered for this machine type.</summary>
    void RegisterImplicit()
    {
        foreach (var declaration in _registrations.Value)
            declaration.Declare(this);
    }

    static IEventRegistration GetEventRegistration(IEvent @event, Type messageType)
    {
        var isFault = messageType.TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] faultMessageType);

        Type registrationType;
        if (messageType.ImplementsInterface<ICorrelatedBy<Guid>>())
        {
            registrationType = isFault
                ? typeof(CorrelatedFaultEventRegistration<>).MakeGenericType(typeof(TInstance), faultMessageType[0])
                : typeof(CorrelatedEventRegistration<>).MakeGenericType(typeof(TInstance), messageType);
        }
        else
        {
            registrationType = isFault
                ? typeof(UncorrelatedFaultEventRegistration<>).MakeGenericType(typeof(TInstance), faultMessageType[0])
                : typeof(UncorrelatedEventRegistration<>).MakeGenericType(typeof(TInstance), messageType);
        }

        return CreateRegistration(registrationType, @event, messageType);
    }

    static IEventRegistration CreateRegistration(Type registrationType, IEvent @event, Type messageType)
    {
        var constructorInfo = registrationType.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 1);
        if (constructorInfo == null)
            throw new ArgumentException("The event correlation could not be created: " + TypeCache.GetShortName(registrationType));

        var eventParameter = Expression.Parameter(typeof(IEvent), "event");
        var convertExpression = Expression.Convert(eventParameter, typeof(IEvent<>).MakeGenericType(messageType));
        var @new = Expression.New(constructorInfo, convertExpression);

        Func<IEvent, IEventRegistration> factoryMethod = Expression.Lambda<Func<IEvent, IEventRegistration>>(@new, eventParameter).CompileFast();

        return factoryMethod(@event);
    }

    IStateMachine<TInstance> Modify(Action<IStateMachineModifier<TInstance>> modifier)
    {
        IStateMachineModifier<TInstance> builder = new StateMachineModifier<TInstance>(this);
        modifier(builder);
        builder.Apply();

        return this;
    }

    /// <summary>Creates a state machine and applies the supplied modifier configuration.</summary>
    /// <param name="modifier">The callback that configures the new machine through its modifier.</param>
    /// <returns>The machine after the modifier has applied its declarations.</returns>
    public static ViciOneServiceBusStateMachine<TInstance> New(Action<IStateMachineModifier<TInstance>> modifier)
    {
        var machine = new BuilderStateMachine();
        machine.Modify(modifier);
        return machine;
    }

    IStateMachineRegistration[] GetRegistrations()
    {
        var events = new List<IStateMachineRegistration>();

        var machineType = GetType();

        IEnumerable<PropertyInfo> properties = GetStateMachineProperties();

        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.PropertyType.IsGenericType)
            {
                if (propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IEvent<>))
                {
                    var declarationType = typeof(DataEventRegistration<,>).MakeGenericType(typeof(TInstance), machineType,
                        propertyInfo.PropertyType.GetGenericArguments().First());
                    var declaration = Activator.CreateInstance(declarationType, propertyInfo) as IStateMachineRegistration
                        ?? throw new InvalidOperationException($"Could not create an event registration for '{propertyInfo.Name}'.");
                    events.Add(declaration);
                }
            }
            else
            {
                if (propertyInfo.PropertyType == typeof(IEvent))
                {
                    var declarationType = typeof(TriggerEventRegistration<>).MakeGenericType(typeof(TInstance), machineType);
                    var declaration = Activator.CreateInstance(declarationType, propertyInfo) as IStateMachineRegistration
                        ?? throw new InvalidOperationException($"Could not create an event registration for '{propertyInfo.Name}'.");
                    events.Add(declaration);
                }
                else if (propertyInfo.PropertyType == typeof(IState))
                {
                    var declarationType = typeof(StateRegistration<>).MakeGenericType(typeof(TInstance), machineType);
                    var declaration = Activator.CreateInstance(declarationType, propertyInfo) as IStateMachineRegistration
                        ?? throw new InvalidOperationException($"Could not create a state registration for '{propertyInfo.Name}'.");
                    events.Add(declaration);
                }
            }
        }

        return events.ToArray();
    }

    IEnumerable<PropertyInfo> GetStateMachineProperties()
    {
        return _stateMachineProperties ??= GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.CanRead && (x.CanWrite || TryGetBackingField(x, out _))).ToList();
    }

    bool TryGetBackingField(PropertyInfo property, [NotNullWhen(true)] out FieldInfo? backingField)
    {
        _backingFields ??= GetBackingFields(GetType())
            .Where(field =>
                field.Attributes.HasFlag(FieldAttributes.Private) &&
                field.Attributes.HasFlag(FieldAttributes.InitOnly) &&
                field.CustomAttributes.Any(attr => attr.AttributeType == typeof(CompilerGeneratedAttribute)) &&
                field.Name.StartsWith("<")
            ).ToList();

        backingField = _backingFields
            .FirstOrDefault(field =>
                field.DeclaringType == property.DeclaringType &&
                field.FieldType.IsAssignableFrom(property.PropertyType) &&
                field.Name.StartsWith("<" + property.Name + ">")
            );

        return backingField != null;
    }

    static IEnumerable<FieldInfo> GetBackingFields(Type type)
    {
        while (true)
        {
            foreach (var fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                yield return fieldInfo;

            if (type.BaseType == null)
                break;

            if (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(ViciOneServiceBusStateMachine<>))
                break;

            type = type.BaseType;
        }
    }

    void InitializeState(ViciOneServiceBusStateMachine<TInstance> stateMachine, PropertyInfo property, StateMachineState state)
    {
        if (property.CanWrite)
            property.SetValue(stateMachine, state);
        else if (TryGetBackingField(property, out var backingField))
            backingField.SetValue(stateMachine, state);
        else
            throw new ArgumentException($"The state property is not writable: {property.Name}");
    }

    void InitializeStateProperty<TProperty>(PropertyInfo stateProperty, TProperty propertyValue, StateMachineState state)
        where TProperty : class
    {
        if (stateProperty.CanWrite)
            stateProperty.SetValue(propertyValue, state);
        else
        {
            var objectProperty = propertyValue.GetType().GetProperty(stateProperty.Name, typeof(IState));
            if (objectProperty == null || !objectProperty.CanWrite)
                throw new ArgumentException($"The state property is not writable: {stateProperty.Name}");

            objectProperty.SetValue(propertyValue, state);
        }
    }

    void InitializeEvent(ViciOneServiceBusStateMachine<TInstance> stateMachine, PropertyInfo property, IEvent @event)
    {
        if (property.CanWrite)
            property.SetValue(stateMachine, @event);
        else if (TryGetBackingField(property, out var backingField))
            backingField.SetValue(stateMachine, @event);
        else
            throw new ArgumentException($"The event property is not writable: {property.Name}");
    }

    void InitializeEventProperty<TProperty, T>(PropertyInfo eventProperty, TProperty propertyValue, IEvent @event)
        where TProperty : class
        where T : class
    {
        if (eventProperty.CanWrite)
            eventProperty.SetValue(propertyValue, @event);
        else
        {
            var objectProperty = propertyValue.GetType().GetProperty(eventProperty.Name, typeof(IEvent<T>));
            if (objectProperty == null || !objectProperty.CanWrite)
                throw new ArgumentException($"The event property is not writable: {eventProperty.Name}");

            objectProperty.SetValue(propertyValue, @event);
        }
    }


    interface IStateMachineRegistration
    {
        void Declare(object stateMachine);
    }


    class StateRegistration<TStateMachine> :
        IStateMachineRegistration
        where TStateMachine : ViciOneServiceBusStateMachine<TInstance>
    {
        readonly PropertyInfo _propertyInfo;

        public StateRegistration(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public void Declare(object stateMachine)
        {
            var machine = (TStateMachine)stateMachine;
            var existing = _propertyInfo.GetValue(machine);
            if (existing != null)
                return;

            machine.DeclareState(_propertyInfo);
        }
    }


    class TriggerEventRegistration<TStateMachine> :
        IStateMachineRegistration
        where TStateMachine : ViciOneServiceBusStateMachine<TInstance>
    {
        readonly PropertyInfo _propertyInfo;

        public TriggerEventRegistration(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public void Declare(object stateMachine)
        {
            var machine = (TStateMachine)stateMachine;
            var existing = _propertyInfo.GetValue(machine);
            if (existing != null)
                return;

            machine.DeclarePropertyBasedEvent(prop => machine.DeclareTriggerEvent(prop.Name), _propertyInfo);
        }
    }


    class DataEventRegistration<TStateMachine, TData> :
        IStateMachineRegistration
        where TStateMachine : ViciOneServiceBusStateMachine<TInstance>
        where TData : class
    {
        readonly PropertyInfo _propertyInfo;

        public DataEventRegistration(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public void Declare(object stateMachine)
        {
            var machine = (TStateMachine)stateMachine;
            var existing = _propertyInfo.GetValue(machine);
            if (existing != null)
                return;

            IEvent<TData> @event = machine.DeclarePropertyBasedEvent(prop => machine.DeclareDataEvent<TData>(prop.Name), _propertyInfo);

            var eventRegistration = GetEventRegistration(@event, typeof(TData));
            eventRegistration.RegisterCorrelation(machine);
        }
    }


    class CorrelatedEventRegistration<TData> :
        IEventRegistration
        where TData : class, ICorrelatedBy<Guid>
    {
        readonly IEvent<TData> _event;

        public CorrelatedEventRegistration(IEvent<TData> @event)
        {
            _event = @event;
        }

        public void RegisterCorrelation(ViciOneServiceBusStateMachine<TInstance> machine)
        {
            var builder = new CorrelatedByEventCorrelationBuilder<TInstance, TData>(machine, _event);

            machine._eventCorrelations[_event] = builder.Build();
        }
    }


    class CorrelatedFaultEventRegistration<TData> :
        IEventRegistration
        where TData : class, ICorrelatedBy<Guid>
    {
        readonly IEvent<Fault<TData>> _event;

        public CorrelatedFaultEventRegistration(IEvent<Fault<TData>> @event)
        {
            _event = @event;
        }

        public void RegisterCorrelation(ViciOneServiceBusStateMachine<TInstance> machine)
        {
            var builder = new CorrelatedByFaultEventCorrelationBuilder<TInstance, TData>(machine, _event);

            machine._eventCorrelations[_event] = builder.Build();
        }
    }


    class UncorrelatedEventRegistration<TData> :
        IEventRegistration
        where TData : class
    {
        readonly IEvent<TData> _event;

        public UncorrelatedEventRegistration(IEvent<TData> @event)
        {
            _event = @event;
        }

        public void RegisterCorrelation(ViciOneServiceBusStateMachine<TInstance> machine)
        {
            if (GlobalTopology.Send.GetMessageTopology<TData>().TryGetConvention(out ICorrelationIdMessageSendTopologyConvention<TData>? convention)
                && convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<TData>? messageCorrelationId))
            {
                var builder = new StateMachineInterfaceType<TInstance, TData>.MessageCorrelationIdEventCorrelationBuilder(machine, _event,
                    messageCorrelationId);

                machine._eventCorrelations[_event] = builder.Build();
            }
            else
                machine._eventCorrelations[_event] = new UncorrelatedEventCorrelation<TData>(_event);
        }
    }


    class UncorrelatedFaultEventRegistration<TData> :
        IEventRegistration
        where TData : class
    {
        readonly IEvent<Fault<TData>> _event;

        public UncorrelatedFaultEventRegistration(IEvent<Fault<TData>> @event)
        {
            _event = @event;
        }

        public void RegisterCorrelation(ViciOneServiceBusStateMachine<TInstance> machine)
        {
            if (GlobalTopology.Send.GetMessageTopology<TData>().TryGetConvention(out ICorrelationIdMessageSendTopologyConvention<TData>? convention)
                && convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<TData>? messageCorrelationId))
            {
                var builder = new StateMachineInterfaceType<TInstance, TData>.MessageCorrelationIdFaultEventCorrelationBuilder(machine, _event,
                    messageCorrelationId);

                machine._eventCorrelations[_event] = builder.Build();
            }
            else
                machine._eventCorrelations[_event] = new UncorrelatedEventCorrelation<Fault<TData>>(_event);
        }
    }


    class BuilderStateMachine :
        ViciOneServiceBusStateMachine<TInstance>
    {
    }


    interface IEventRegistration
    {
        void RegisterCorrelation(ViciOneServiceBusStateMachine<TInstance> machine);
    }
}
