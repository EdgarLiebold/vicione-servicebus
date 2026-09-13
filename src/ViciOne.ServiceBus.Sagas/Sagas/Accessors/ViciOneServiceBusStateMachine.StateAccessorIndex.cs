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
            _stateMachine = stateMachine;

            _assignedStates = new[] { null, initial, final }.Concat(states.Cast<IState<TInstance>>()).ToArray();

            _states = new Lazy<IState<TInstance>?[]>(CreateStateArray);
        }

        public int this[string name]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentNullException(nameof(name));

                for (var i = 1; i < _states.Value.Length; i++)
                {
                    if (_states.Value[i]?.Name.Equals(name) == true)
                        return i;
                }

                throw new ArgumentException("Unknown state specified: " + name);
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
