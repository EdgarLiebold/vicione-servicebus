using System;
using System.Linq;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    class StateAccessorIndex
    {
        readonly IState<TInstance>?[] _assignedStates;
        readonly IStateMachine<TInstance> _stateMachine;
        readonly Lazy<IState<TInstance>?[]> _states;

        public StateAccessorIndex(IStateMachine<TInstance> stateMachine, IState<TInstance> initial, IState<TInstance> final, IState[] states)
        {
            ArgumentNullException.ThrowIfNull(stateMachine);
            ArgumentNullException.ThrowIfNull(initial);
            ArgumentNullException.ThrowIfNull(final);
            ArgumentNullException.ThrowIfNull(states);
            if (states.Any(state => state is null))
                throw new ArgumentException("States must not contain null values.", nameof(states));

            _stateMachine = stateMachine;

            IState<TInstance>[] typedStates = states.Select(state => state as IState<TInstance>
                ?? throw new ArgumentException("States must be compatible with the saga instance type.", nameof(states))).ToArray();
            _assignedStates = new[] { null, initial, final }.Concat(typedStates).ToArray();

            _states = new Lazy<IState<TInstance>?[]>(CreateStateArray);
        }

        public int this[string name]
        {
            get
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);

                for (var i = 1; i < _states.Value.Length; i++)
                {
                    if (_states.Value[i]?.Name.Equals(name) == true)
                        return i;
                }

                throw new ArgumentException("Unknown state specified: " + name, nameof(name));
            }
        }

        public IState<TInstance>? this[int index]
        {
            get
            {
                if (index < 0 || index >= _states.Value.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _states.Value[index];
            }
        }

        IState<TInstance>?[] CreateStateArray()
        {
            return _assignedStates.Concat(_stateMachine.States.Cast<IState<TInstance>>()).Distinct().ToArray();
        }
    }
}
