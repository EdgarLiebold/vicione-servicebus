using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    class InitialIfNullStateAccessor :
        IStateAccessor<TInstance>
    {
        readonly IBehavior<TInstance> _initialBehavior;
        readonly IStateAccessor<TInstance> _stateAccessor;

        public InitialIfNullStateAccessor(IState<TInstance> initialState, IStateAccessor<TInstance> stateAccessor)
        {
            ArgumentNullException.ThrowIfNull(initialState);
            ArgumentNullException.ThrowIfNull(stateAccessor);

            _stateAccessor = stateAccessor;

            IStateMachineActivity<TInstance> initialActivity = new TransitionActivity<TInstance>(initialState, _stateAccessor);
            _initialBehavior = new LastBehavior<TInstance>(initialActivity);
        }

        async Task<IState<TInstance>?> IStateAccessor<TInstance>.GetAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            IState<TInstance>? state = await _stateAccessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                await _initialBehavior.ExecuteAsync(context).ConfigureAwait(false);

                state = await _stateAccessor.GetAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return state;
        }

        Task IStateAccessor<TInstance>.SetAsync(IBehaviorContext<TInstance> context, IState<TInstance> state, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(state);
            return _stateAccessor.SetAsync(context, state, cancellationToken: cancellationToken);
        }

        public Expression<Func<TInstance, bool>> GetStateExpression(params IState[] states)
        {
            ArgumentNullException.ThrowIfNull(states);
            return _stateAccessor.GetStateExpression(states);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _stateAccessor.Probe(context);
        }
    }
}
