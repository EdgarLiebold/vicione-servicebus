using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// The default state accessor will attempt to find and use a single State property on the
    /// instance type. If no State property is found, or more than one is found, an exception
    /// will be thrown.
    /// </summary>
    class DefaultInstanceStateAccessor :
        IStateAccessor<TInstance>
    {
        readonly Lazy<IStateAccessor<TInstance>> _accessor;
        readonly IState<TInstance> _initialState;
        readonly IStateMachine<TInstance> _machine;
        readonly IStateObserver<TInstance> _observer;

        public DefaultInstanceStateAccessor(IStateMachine<TInstance> machine, IState<TInstance> initialState, IStateObserver<TInstance> observer)
        {
            _machine = machine;
            _initialState = initialState;
            _observer = observer;
            _accessor = new Lazy<IStateAccessor<TInstance>>(CreateDefaultAccessor);
        }

        Task<IState<TInstance>?> IStateAccessor<TInstance>.GetAsync(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
        {
            return _accessor.Value.GetAsync(context, cancellationToken: cancellationToken);
        }

        Task IStateAccessor<TInstance>.SetAsync(IBehaviorContext<TInstance> context, IState<TInstance> state, CancellationToken cancellationToken)
        {
            return _accessor.Value.SetAsync(context, state, cancellationToken: cancellationToken);
        }

        public Expression<Func<TInstance, bool>> GetStateExpression(params IState[] states)
        {
            return _accessor.Value.GetStateExpression(states);
        }

        public void Probe(ProbeContext context)
        {
            _accessor.Value.Probe(context);
        }

        IStateAccessor<TInstance> CreateDefaultAccessor()
        {
            List<PropertyInfo> states = typeof(TInstance)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.PropertyType == typeof(IState))
                .Where(x => x.GetGetMethod(true) != null)
                .Where(x => x.GetSetMethod(true) != null)
                .ToList();

            if (states.Count > 1)
            {
                throw new SagaStateMachineException(
                    "The InstanceState was not configured, and could not be automatically identified as multiple State properties exist.");
            }

            if (states.Count == 0)
            {
                throw new SagaStateMachineException(
                    "The InstanceState was not configured, and no public State property exists.");
            }

            var instance = Expression.Parameter(typeof(TInstance), "instance");
            var memberExpression = Expression.Property(instance, states[0]);

            Expression<Func<TInstance, IState?>> expression = Expression.Lambda<Func<TInstance, IState?>>(memberExpression,
                instance);

            return new InitialIfNullStateAccessor(_initialState, new RawStateAccessor(_machine, expression, _observer));
        }
    }
}
