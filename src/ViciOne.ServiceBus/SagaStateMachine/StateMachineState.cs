using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides a state machine state implementation.
    /// </summary>
    public class StateMachineState :
        State<TInstance>,
        IEquatable<State>
    {
        readonly Dictionary<Event, ActivityBehaviorBuilder<TInstance>> _behaviors;
        readonly Dictionary<Event, IStateEventFilter<TInstance>> _ignoredEvents;
        readonly IEventObserver<TInstance> _observer;
        readonly HashSet<State<TInstance>> _subStates;
        readonly StateMachineUnhandledEventCallback<TInstance> _unhandledEventCallback;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="unhandledEventCallback">The unhandled event callback value.</param>
        /// <param name="name">The name value.</param>
        /// <param name="observer">The observer value.</param>
        /// <param name="superState">The super state value.</param>
        public StateMachineState(StateMachineUnhandledEventCallback<TInstance> unhandledEventCallback, string name, IEventObserver<TInstance> observer,
            State<TInstance>? superState = null)
        {
            _unhandledEventCallback = unhandledEventCallback;
            Name = name;
            _observer = observer;

            _behaviors = new Dictionary<Event, ActivityBehaviorBuilder<TInstance>>();
            _ignoredEvents = new Dictionary<Event, IStateEventFilter<TInstance>>();

            Enter = new TriggerEvent(name + ".Enter");
            Ignore(Enter);
            Leave = new TriggerEvent(name + ".Leave");
            Ignore(Leave);

            BeforeEnter = new MessageEvent<State>(name + ".BeforeEnter");
            Ignore(BeforeEnter);
            AfterLeave = new MessageEvent<State>(name + ".AfterLeave");
            Ignore(AfterLeave);

            _subStates = new HashSet<State<TInstance>>();

            SuperState = superState;
            superState?.AddSubstate(this);
        }

        /// <summary>
        /// Determines whether this instance equals the supplied value.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool Equals(State? other)
        {
            return string.CompareOrdinal(Name, other?.Name ?? "") == 0;
        }

        /// <summary>
        /// Gets the super state value.
        /// </summary>
        public State<TInstance>? SuperState { get; }
        /// <summary>
        /// Gets the name value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the enter value.
        /// </summary>
        public Event Enter { get; }
        /// <summary>
        /// Gets the leave value.
        /// </summary>
        public Event Leave { get; }
        /// <summary>
        /// Gets the before enter value.
        /// </summary>
        public Event<State> BeforeEnter { get; }
        /// <summary>
        /// Gets the after leave value.
        /// </summary>
        public Event<State> AfterLeave { get; }

        /// <summary>
        /// Performs the accept operation.
        /// </summary>
        /// <param name="visitor">The visitor value.</param>
        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this, _ =>
            {
                foreach (KeyValuePair<Event, ActivityBehaviorBuilder<TInstance>> behavior in _behaviors)
                {
                    behavior.Key.Accept(visitor);
                    behavior.Value.Behavior.Accept(visitor);
                }
            });
        }

        /// <summary>
        /// Performs the probe operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("state");
            scope.Add("name", Name);

            if (_subStates.Any())
            {
                var subStateScope = scope.CreateScope("substates");
                foreach (State<TInstance> subState in _subStates)
                    subStateScope.Add("name", subState.Name);
            }

            if (_behaviors.Any())
            {
                foreach (KeyValuePair<Event, ActivityBehaviorBuilder<TInstance>> behavior in _behaviors)
                {
                    var eventScope = scope.CreateScope("event");
                    behavior.Key.Probe(eventScope);

                    behavior.Value.Behavior.Probe(eventScope.CreateScope("behavior"));
                }
            }

            List<KeyValuePair<Event, IStateEventFilter<TInstance>>> ignored = _ignoredEvents.Where(x => IsRealEvent(x.Key)).ToList();
            if (ignored.Any())
            {
                foreach (KeyValuePair<Event, IStateEventFilter<TInstance>> ignoredEvent in ignored)
                    ignoredEvent.Key.Probe(scope.CreateScope("event-ignored"));
            }
        }

        async Task State<TInstance>.RaiseAsync(BehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
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
                        // the exception is better if it's from the substate
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
            catch (Exception ex)
            {
                await _observer.ExecuteFaultAsync(context, ex).ConfigureAwait(false);

                throw;
            }
        }

        async Task State<TInstance>.RaiseAsync<T>(BehaviorContext<TInstance, T> context, CancellationToken cancellationToken)
        {
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
                        // the exception is better if it's from the substate
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
            catch (Exception ex)
            {
                await _observer.ExecuteFaultAsync(context, ex).ConfigureAwait(false);

                throw;
            }
        }

        /// <summary>
        /// Performs the bind operation.
        /// </summary>
        /// <param name="event">The event value.</param>
        /// <param name="activity">The activity value.</param>
        public void Bind(Event @event, IStateMachineActivity<TInstance> activity)
        {
            if (!_behaviors.TryGetValue(@event, out ActivityBehaviorBuilder<TInstance>? builder))
            {
                builder = new ActivityBehaviorBuilder<TInstance>();
                _behaviors.Add(@event, builder);
            }

            builder.Add(activity);
        }

        /// <summary>
        /// Performs the ignore operation.
        /// </summary>
        /// <param name="event">The event value.</param>
        public void Ignore(Event @event)
        {
            _ignoredEvents[@event] = new AllStateEventFilter<TInstance>();
        }

        /// <summary>
        /// Performs the ignore operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="event">The event value.</param>
        /// <param name="filter">The filter value.</param>
        public void Ignore<T>(Event<T> @event, StateMachineCondition<TInstance, T> filter)
            where T : class
        {
            _ignoredEvents[@event] = new SelectedStateEventFilter<TInstance, T>(filter);
        }

        /// <summary>
        /// Adds substate to the configuration.
        /// </summary>
        /// <param name="subState">The sub state value.</param>
        public void AddSubstate(State<TInstance> subState)
        {
            if (subState == null)
                throw new ArgumentNullException(nameof(subState));

            if (Name.Equals(subState.Name))
                throw new ArgumentException("A state cannot be a substate of itself", nameof(subState));

            _subStates.Add(subState);
        }

        /// <summary>
        /// Determines whether the current value has state.
        /// </summary>
        /// <param name="state">The state value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool HasState(State<TInstance> state)
        {
            return Name.Equals(state.Name) || _subStates.Any(s => s.HasState(state));
        }

        /// <summary>
        /// Determines whether state of.
        /// </summary>
        /// <param name="state">The state value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool IsStateOf(State<TInstance> state)
        {
            return Name.Equals(state.Name) || (SuperState != null && SuperState.IsStateOf(state));
        }

        /// <summary>
        /// Gets the events value.
        /// </summary>
        public IEnumerable<Event> Events => SuperState != null ? SuperState.Events.Union(GetStateEvents()).Distinct() : GetStateEvents();

        /// <summary>
        /// Compares this instance with the supplied value.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The result of the operation.</returns>
        public int CompareTo(State? other)
        {
            return other == null ? 1 : string.CompareOrdinal(Name, other.Name);
        }

        bool IsRealEvent(Event @event)
        {
            if (Equals(@event, Enter) || Equals(@event, Leave) || Equals(@event, BeforeEnter) || Equals(@event, AfterLeave))
                return false;

            return true;
        }

        IEnumerable<Event> GetStateEvents()
        {
            return _behaviors.Keys
                .Union(_ignoredEvents.Keys)
                .Where(IsRealEvent)
                .Distinct();
        }

        /// <summary>
        /// Determines whether this instance equals the supplied value.
        /// </summary>
        /// <param name="obj">The obj value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as State;
            return other != null && Equals(other);
        }

        /// <summary>
        /// Gets hash code.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Applies the <c>==</c> operator.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(State<TInstance> left, StateMachineState right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Applies the <c>!=</c> operator.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(State<TInstance> left, StateMachineState right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Applies the <c>==</c> operator.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(StateMachineState left, State<TInstance> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Applies the <c>!=</c> operator.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(StateMachineState left, State<TInstance> right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Applies the <c>==</c> operator.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(StateMachineState left, StateMachineState right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Applies the <c>!=</c> operator.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(StateMachineState left, StateMachineState right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns the string representation of this instance.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override string ToString()
        {
            return $"{Name} (State)";
        }
    }
}
