using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Describes a saga message schedule and the property that stores its cancellation token ID.</summary>
    /// <typeparam name="TMessage">The scheduled message contract.</typeparam>
    public class StateMachineSchedule<TMessage> :
        ISchedule<TInstance, TMessage>
        where TMessage : class
    {
        readonly string _name;
        readonly IReadProperty<TInstance, Guid?> _read;
        readonly IScheduleSettings<TInstance, TMessage> _settings;
        readonly IWriteProperty<TInstance, Guid?> _write;

        /// <summary>Configures the schedule's name, delay provider and readable/writable token-ID property.</summary>
        /// <param name="name">The schedule's state-machine name.</param>
        /// <param name="tokenIdExpression">The saga property used to store the scheduled message's token ID.</param>
        /// <param name="settings">The settings supplying the schedule's delay provider.</param>
        public StateMachineSchedule(string name, Expression<Func<TInstance, Guid?>> tokenIdExpression, IScheduleSettings<TInstance, TMessage> settings)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(tokenIdExpression);
            ArgumentNullException.ThrowIfNull(settings);

            _name = name;
            _settings = settings;

            var propertyInfo = tokenIdExpression.GetPropertyInfo();

            _read = ReadPropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
            _write = WritePropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
        }

        string ISchedule<TInstance>.Name => _name;
        /// <summary>Gets or sets the correlated scheduled-message event assigned during schedule configuration.</summary>
        public IEvent<TMessage> Received { get; set; } = null!;
        /// <summary>Gets or sets the general scheduled-message event assigned during schedule configuration.</summary>
        public IEvent<TMessage> AnyReceived { get; set; } = null!;
        /// <summary>Evaluates the configured delay provider for the current saga context.</summary>
        /// <param name="context">The saga and event used by the delay provider.</param>
        /// <returns>The configured provider's delay.</returns>
        public TimeSpan GetDelay(IBehaviorContext<TInstance> context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return _settings.DelayProvider(context);
        }

        /// <summary>Reads the scheduled-message token ID from the configured saga property.</summary>
        /// <param name="instance">The saga whose token-ID property is read.</param>
        /// <returns>The property's token ID, or <see langword="null" /> when it is unset.</returns>
        public Guid? GetTokenId(TInstance instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return _read.Get(instance);
        }

        /// <summary>Writes or clears the scheduled-message token ID in the configured saga property.</summary>
        /// <param name="instance">The saga whose token-ID property is written.</param>
        /// <param name="tokenId">The token ID to store, or <see langword="null" /> to clear it.</param>
        public void SetTokenId(TInstance instance, Guid? tokenId)
        {
            ArgumentNullException.ThrowIfNull(instance);

            _write.Set(instance, tokenId);
        }
    }
}
