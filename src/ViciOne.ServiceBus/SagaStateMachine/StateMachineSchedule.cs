using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides a state machine schedule implementation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    public class StateMachineSchedule<TMessage> :
        Schedule<TInstance, TMessage>
        where TMessage : class
    {
        readonly string _name;
        readonly IReadProperty<TInstance, Guid?> _read;
        readonly ScheduleSettings<TInstance, TMessage> _settings;
        readonly IWriteProperty<TInstance, Guid?> _write;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="name">The name value.</param>
        /// <param name="tokenIdExpression">The token id expression value.</param>
        /// <param name="settings">The settings value.</param>
        public StateMachineSchedule(string name, Expression<Func<TInstance, Guid?>> tokenIdExpression, ScheduleSettings<TInstance, TMessage> settings)
        {
            _name = name;
            _settings = settings;

            var propertyInfo = tokenIdExpression.GetPropertyInfo();

            _read = ReadPropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
            _write = WritePropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
        }

        string Schedule<TInstance>.Name => _name;
        /// <summary>
        /// Gets or sets the received value.
        /// </summary>
        public Event<TMessage> Received { get; set; } = null!;
        /// <summary>
        /// Gets or sets the any received value.
        /// </summary>
        public Event<TMessage> AnyReceived { get; set; } = null!;
        /// <summary>
        /// Gets delay.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public TimeSpan GetDelay(BehaviorContext<TInstance> context)
        {
            return _settings.DelayProvider(context);
        }

        /// <summary>
        /// Gets token id.
        /// </summary>
        /// <param name="instance">The instance value.</param>
        /// <returns>The result of the operation.</returns>
        public Guid? GetTokenId(TInstance instance)
        {
            return _read.Get(instance);
        }

        /// <summary>
        /// Sets token id.
        /// </summary>
        /// <param name="instance">The instance value.</param>
        /// <param name="tokenId">The token id value.</param>
        public void SetTokenId(TInstance instance, Guid? tokenId)
        {
            _write.Set(instance, tokenId);
        }
    }
}
