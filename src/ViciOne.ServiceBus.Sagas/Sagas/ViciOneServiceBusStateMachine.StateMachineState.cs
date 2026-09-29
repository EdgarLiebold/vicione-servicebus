using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Stores a named saga state's event behaviors, ignored events and state hierarchy.</summary>
    public class StateMachineState :
        IState<TInstance>,
        IEquatable<IState>
    {
        readonly Dictionary<IEvent, ActivityBehaviorBuilder<TInstance>> _behaviors;
        readonly Dictionary<IEvent, IStateEventFilter<TInstance>> _ignoredEvents;
        readonly IEventObserver<TInstance> _observer;
        readonly HashSet<IState<TInstance>> _subStates;
        readonly StateMachineUnhandledEventCallback<TInstance> _unhandledEventCallback;

        /// <summary>Creates the state's transition events and registers it with an optional parent state.</summary>
        /// <param name="unhandledEventCallback">The callback used when neither this state nor its parent handles an event.</param>
        /// <param name="name">The state's name and prefix for its transition event names.</param>
        /// <param name="observer">The observer notified around execution of a bound event behavior.</param>
        /// <param name="superState">The parent state, or <see langword="null" /> for a top-level state.</param>
        public StateMachineState(StateMachineUnhandledEventCallback<TInstance> unhandledEventCallback, string name, IEventObserver<TInstance> observer,
            IState<TInstance>? superState = null)
        {
            ArgumentNullException.ThrowIfNull(unhandledEventCallback);
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(observer);

            _unhandledEventCallback = unhandledEventCallback;
            Name = name;
            _observer = observer;

            _behaviors = new Dictionary<IEvent, ActivityBehaviorBuilder<TInstance>>();
            _ignoredEvents = new Dictionary<IEvent, IStateEventFilter<TInstance>>();

            Enter = new TriggerEvent(name + ".Enter");
            Ignore(Enter);
            Leave = new TriggerEvent(name + ".Leave");
            Ignore(Leave);

            BeforeEnter = new MessageEvent<IState>(name + ".BeforeEnter");
            Ignore(BeforeEnter);
            AfterLeave = new MessageEvent<IState>(name + ".AfterLeave");
            Ignore(AfterLeave);

            _subStates = new HashSet<IState<TInstance>>();

            SuperState = superState;
            superState?.AddSubstate(this);
        }

        /// <summary>Compares non-null state names using ordinal equality.</summary>
        /// <param name="other">The state whose name is compared.</param>
        /// <returns><see langword="true" /> when the other state is non-null and has the same ordinal name; otherwise, <see langword="false" />.</returns>
        public bool Equals(IState? other)
        {
            return other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <summary>Gets the parent state, or <see langword="null" /> for a top-level state.</summary>
        public IState<TInstance>? SuperState { get; private set; }
        /// <summary>Gets the name used for state equality, ordering and transition-event names.</summary>
        public string Name { get; }

        /// <summary>Gets the trigger event associated with entering this state.</summary>
        public IEvent Enter { get; }
        /// <summary>Gets the trigger event associated with leaving this state.</summary>
        public IEvent Leave { get; }
        /// <summary>Gets the before-enter event carrying state data for the transition.</summary>
        public IEvent<IState> BeforeEnter { get; }
        /// <summary>Gets the after-leave event carrying state data for the transition.</summary>
        public IEvent<IState> AfterLeave { get; }

        /// <summary>Visits this state and the events and behaviors bound directly to it.</summary>
        /// <param name="visitor">The visitor receiving the state, bound events and behavior graph.</param>
        public void Accept(IStateMachineVisitor visitor)
        {
            ArgumentNullException.ThrowIfNull(visitor);

            visitor.Visit(this, _ =>
            {
                foreach (KeyValuePair<IEvent, ActivityBehaviorBuilder<TInstance>> behavior in _behaviors)
                {
                    behavior.Key.Accept(visitor);
                    behavior.Value.Behavior.Accept(visitor);
                }
            });
        }

        /// <summary>Reports the state's name, substates, bound behaviors and ignored non-transition events.</summary>
        /// <param name="context">The parent diagnostic scope in which the state scope is created.</param>
        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var scope = context.CreateScope("state");
            scope.Add("name", Name);

            if (_subStates.Any())
            {
                var subStateScope = scope.CreateScope("substates");
                foreach (IState<TInstance> subState in _subStates)
                    subStateScope.Add("name", subState.Name);
            }

            if (_behaviors.Any())
            {
                foreach (KeyValuePair<IEvent, ActivityBehaviorBuilder<TInstance>> behavior in _behaviors)
                {
                    var eventScope = scope.CreateScope("event");
                    behavior.Key.Probe(eventScope);

                    behavior.Value.Behavior.Probe(eventScope.CreateScope("behavior"));
                }
            }

            List<KeyValuePair<IEvent, IStateEventFilter<TInstance>>> ignored = _ignoredEvents.Where(x => IsRealEvent(x.Key)).ToList();
            if (ignored.Any())
            {
                foreach (KeyValuePair<IEvent, IStateEventFilter<TInstance>> ignoredEvent in ignored)
                    ignoredEvent.Key.Probe(scope.CreateScope("event-ignored"));
            }
        }

        async Task IState<TInstance>.RaiseAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            if (!_behaviors.TryGetValue(context.Event, out ActivityBehaviorBuilder<TInstance>? activities))
            {
                if (_ignoredEvents.TryGetValue(context.Event, out IStateEventFilter<TInstance>? filter) && filter.Filter(context))
                    return;

                if (SuperState != null)
                {
                    try
                    {
                        await SuperState.RaiseAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (UnhandledEventException)
                    {
                        // The substate owns the more specific unhandled-event decision.
                    }
                }

                await _unhandledEventCallback(context, this).ConfigureAwait(false);
                return;
            }

            try
            {
                await _observer.PreExecuteAsync(context).ConfigureAwait(false);

                await activities.Behavior.ExecuteAsync(context).ConfigureAwait(false);

                await _observer.PostExecuteAsync(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _observer.ExecuteFaultAsync(context, ex).ConfigureAwait(false);

                throw;
            }
        }

        async Task IState<TInstance>.RaiseAsync<T>(IBehaviorContext<TInstance, T> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            if (!_behaviors.TryGetValue(context.Event, out ActivityBehaviorBuilder<TInstance>? activities))
            {
                if (_ignoredEvents.TryGetValue(context.Event, out IStateEventFilter<TInstance>? filter) && filter.Filter(context))
                    return;

                if (SuperState != null)
                {
                    try
                    {
                        await SuperState.RaiseAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (UnhandledEventException)
                    {
                        // The substate owns the more specific unhandled-event decision.
                    }
                }

                await _unhandledEventCallback(context, this).ConfigureAwait(false);
                return;
            }

            try
            {
                await _observer.PreExecuteAsync(context).ConfigureAwait(false);

                await activities.Behavior.ExecuteAsync(context).ConfigureAwait(false);

                await _observer.PostExecuteAsync(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _observer.ExecuteFaultAsync(context, ex).ConfigureAwait(false);

                throw;
            }
        }

        /// <summary>Appends an activity to the behavior bound directly to an event in this state.</summary>
        /// <param name="event">The event whose behavior receives the activity.</param>
        /// <param name="activity">The activity appended to that event's behavior builder.</param>
        public void Bind(IEvent @event, IStateMachineActivity<TInstance> activity)
        {
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));
            ArgumentNullException.ThrowIfNull(activity);

            if (!_behaviors.TryGetValue(@event, out ActivityBehaviorBuilder<TInstance>? builder))
            {
                builder = new ActivityBehaviorBuilder<TInstance>();
                _behaviors.Add(@event, builder);
            }

            builder.Add(activity);
        }

        /// <summary>Configures an event to be ignored when no behavior is bound directly to it.</summary>
        /// <param name="event">The event whose unbound occurrences are ignored.</param>
        public void Ignore(IEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));

            _ignoredEvents[@event] = new AllStateEventFilter<TInstance>();
        }

        /// <summary>Configures unbound occurrences of a message event to be ignored when a condition matches.</summary>
        /// <typeparam name="T">The message contract carried by the event.</typeparam>
        /// <param name="event">The message event whose unbound occurrences are filtered.</param>
        /// <param name="filter">The condition deciding whether an occurrence is ignored.</param>
        public void Ignore<T>(IEvent<T> @event, StateMachineCondition<TInstance, T> filter)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));
            ArgumentNullException.ThrowIfNull(filter);

            _ignoredEvents[@event] = new SelectedStateEventFilter<TInstance, T>(filter);
        }

        /// <summary>Adds a substate unless its name equals this state's name.</summary>
        /// <param name="subState">The state added to this state's substate set.</param>
        public void AddSubstate(IState<TInstance> subState)
        {
            if (subState == null)
                throw new ArgumentNullException(nameof(subState));

            if (Name.Equals(subState.Name))
                throw new ArgumentException("A state cannot be a substate of itself", nameof(subState));

            _subStates.Add(subState);
        }

        internal void RemoveSubstate(IState<TInstance> subState) => _subStates.Remove(subState);

        internal void MoveSubstatesTo(StateMachineState replacement)
        {
            foreach (StateMachineState child in _subStates.OfType<StateMachineState>().ToArray())
            {
                _subStates.Remove(child);
                child.SuperState = replacement;
                replacement.AddSubstate(child);
            }
        }

        /// <summary>Searches this state and its descendants for a state with the supplied name.</summary>
        /// <param name="state">The state supplying the name to find.</param>
        /// <returns>Whether this state or a descendant has the supplied name.</returns>
        public bool HasState(IState<TInstance> state)
        {
            ArgumentNullException.ThrowIfNull(state);

            return Name.Equals(state.Name) || _subStates.Any(s => s.HasState(state));
        }

        /// <summary>Searches this state and its ancestors for a state with the supplied name.</summary>
        /// <param name="state">The state supplying the name to find.</param>
        /// <returns>Whether this state or an ancestor has the supplied name.</returns>
        public bool IsStateOf(IState<TInstance> state)
        {
            ArgumentNullException.ThrowIfNull(state);

            return Name.Equals(state.Name) || (SuperState != null && SuperState.IsStateOf(state));
        }

        /// <summary>Gets distinct non-transition events from this state and its ancestors.</summary>
        public IEnumerable<IEvent> Events => SuperState != null ? SuperState.Events.Union(GetStateEvents()).Distinct() : GetStateEvents();

        /// <summary>Gets directly bound events and ignored non-transition events without inherited events.</summary>
        public IEnumerable<IEvent> DeclaredEvents => _behaviors.Keys
            .Union(_ignoredEvents.Keys.Where(IsRealEvent))
            .Distinct();

        /// <summary>Orders states by ordinal name comparison, after a <see langword="null" /> state.</summary>
        /// <param name="other">The state whose name is compared, or <see langword="null" />.</param>
        /// <returns>A negative, zero or positive value indicating name order; one for <see langword="null" />.</returns>
        public int CompareTo(IState? other)
        {
            return other == null ? 1 : string.CompareOrdinal(Name, other.Name);
        }

        bool IsRealEvent(IEvent @event)
        {
            if (Equals(@event, Enter) || Equals(@event, Leave) || Equals(@event, BeforeEnter) || Equals(@event, AfterLeave))
                return false;

            return true;
        }

        IEnumerable<IEvent> GetStateEvents()
        {
            return _behaviors.Keys
                .Union(_ignoredEvents.Keys)
                .Where(IsRealEvent)
                .Distinct();
        }

        /// <summary>Compares a non-null state object with this state using ordinal name equality.</summary>
        /// <param name="obj">The object compared with this state.</param>
        /// <returns>Whether the object is this instance or a state with the same name.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as IState;
            return other != null && Equals(other);
        }

        /// <summary>Gets the state name's hash code, or zero when the name is absent.</summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        /// <summary>Compares a saga state with this concrete state using object equality.</summary>
        /// <param name="left">The saga state on the left.</param>
        /// <param name="right">The concrete state on the right.</param>
        /// <returns>Whether object equality considers the two states equal.</returns>
        public static bool operator ==(IState<TInstance> left, StateMachineState right)
        {
            return Equals(left, right);
        }

        /// <summary>Negates object equality between a saga state and this concrete state.</summary>
        /// <param name="left">The saga state on the left.</param>
        /// <param name="right">The concrete state on the right.</param>
        /// <returns>Whether object equality considers the two states unequal.</returns>
        public static bool operator !=(IState<TInstance> left, StateMachineState right)
        {
            return !Equals(left, right);
        }

        /// <summary>Compares this concrete state with a saga state using object equality.</summary>
        /// <param name="left">The concrete state on the left.</param>
        /// <param name="right">The saga state on the right.</param>
        /// <returns>Whether object equality considers the two states equal.</returns>
        public static bool operator ==(StateMachineState left, IState<TInstance> right)
        {
            return Equals(left, right);
        }

        /// <summary>Negates object equality between this concrete state and a saga state.</summary>
        /// <param name="left">The concrete state on the left.</param>
        /// <param name="right">The saga state on the right.</param>
        /// <returns>Whether object equality considers the two states unequal.</returns>
        public static bool operator !=(StateMachineState left, IState<TInstance> right)
        {
            return !Equals(left, right);
        }

        /// <summary>Compares two concrete states using object equality.</summary>
        /// <param name="left">The concrete state on the left.</param>
        /// <param name="right">The concrete state on the right.</param>
        /// <returns>Whether object equality considers the two states equal.</returns>
        public static bool operator ==(StateMachineState left, StateMachineState right)
        {
            return Equals(left, right);
        }

        /// <summary>Negates object equality between two concrete states.</summary>
        /// <param name="left">The concrete state on the left.</param>
        /// <param name="right">The concrete state on the right.</param>
        /// <returns>Whether object equality considers the two states unequal.</returns>
        public static bool operator !=(StateMachineState left, StateMachineState right)
        {
            return !Equals(left, right);
        }

        /// <summary>Returns the state's name followed by its state marker.</summary>
        /// <returns>The display text in the form <c>Name (State)</c>.</returns>
        public override string ToString()
        {
            return $"{Name} (State)";
        }
    }
}
