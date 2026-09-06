using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Defines the schedule for state machine.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    public class StateMachineSchedule<TMessage> :
        Schedule<TInstance, TMessage>
        where TMessage : class
    {
        readonly string _name;
        readonly IReadProperty<TInstance, Guid?> _read;
        readonly ScheduleSettings<TInstance, TMessage> _settings;
        readonly IWriteProperty<TInstance, Guid?> _write;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="name">The name.</param>
        /// <param name="tokenIdExpression">The token id expression.</param>
        /// <param name="settings">The settings that control the operation.</param>
        public StateMachineSchedule(string name, Expression<Func<TInstance, Guid?>> tokenIdExpression, ScheduleSettings<TInstance, TMessage> settings)
        {
            _name = name;
            _settings = settings;

            var propertyInfo = tokenIdExpression.GetPropertyInfo();

            _read = ReadPropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
            _write = WritePropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
        }

        string Schedule<TInstance>.Name => _name;
        /// <summary>Gets or sets the received.</summary>
        public Event<TMessage> Received { get; set; } = null!;
        /// <summary>Gets or sets the any received.</summary>
        public Event<TMessage> AnyReceived { get; set; } = null!;
        /// <summary>Gets delay.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>The delay.</returns>
        public TimeSpan GetDelay(BehaviorContext<TInstance> context)
        {
            return _settings.DelayProvider(context);
        }

        /// <summary>Gets token id.</summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The token id.</returns>
        public Guid? GetTokenId(TInstance instance)
        {
            return _read.Get(instance);
        }

        /// <summary>Sets token id.</summary>
        /// <param name="instance">The instance.</param>
        /// <param name="tokenId">The token id.</param>
        public void SetTokenId(TInstance instance, Guid? tokenId)
        {
            _write.Set(instance, tokenId);
        }
    }
}
