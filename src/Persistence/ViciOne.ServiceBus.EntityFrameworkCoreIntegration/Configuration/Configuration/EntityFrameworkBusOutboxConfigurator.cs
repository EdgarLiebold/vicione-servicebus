#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Linq;
    using DependencyInjection;
    using EntityFrameworkCoreIntegration;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Middleware.Outbox;
    using Transactions;


    public class EntityFrameworkBusOutboxConfigurator<TDbContext> :
        IEntityFrameworkBusOutboxConfigurator
        where TDbContext : DbContext
    {
        readonly IBusRegistrationConfigurator _configurator;
        readonly EntityFrameworkOutboxConfigurator<TDbContext> _outboxConfigurator;
        bool _registerOutboxDeliveryService;

        public EntityFrameworkBusOutboxConfigurator(IBusRegistrationConfigurator configurator, EntityFrameworkOutboxConfigurator<TDbContext> outboxConfigurator)
        {
            _outboxConfigurator = outboxConfigurator;
            _configurator = configurator;

            _registerOutboxDeliveryService = true;
        }

        /// <summary>
        /// The number of message to deliver at a time from the outbox
        /// </summary>
        public int MessageDeliveryLimit { get; set; } = 100;

        /// <summary>
        /// Transport Send timeout when delivering messages to the transport
        /// </summary>
        public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public void DisableDeliveryService()
        {
            _registerOutboxDeliveryService = false;
        }

        public virtual void Configure(Action<IEntityFrameworkBusOutboxConfigurator>? configure)
        {
            configure?.Invoke(this);

            if (MessageDeliveryLimit <= 0)
                throw new ConfigurationException("MessageDeliveryLimit must be greater than zero.");
            if (MessageDeliveryTimeout <= TimeSpan.Zero)
                throw new ConfigurationException("MessageDeliveryTimeout must be greater than zero.");

            TimeSpan queryDelay = _outboxConfigurator.QueryDelay;
            int queryMessageLimit = _outboxConfigurator.QueryMessageLimit;
            TimeSpan queryTimeout = _outboxConfigurator.QueryTimeout;
            int messageDeliveryLimit = MessageDeliveryLimit;
            TimeSpan messageDeliveryTimeout = MessageDeliveryTimeout;

            Type? conflictingCapability = _configurator
                .Where(descriptor => descriptor.ServiceType == typeof(Bind<IBus, IAmbientTransactionBus>)
                    || descriptor.ServiceType == typeof(Bind<IBus, IBufferedBus>))
                .Select(descriptor => descriptor.ServiceType.GenericTypeArguments[1])
                .FirstOrDefault();
            if (conflictingCapability != null)
            {
                throw new ConfigurationException(
                    $"The Entity Framework bus outbox cannot be combined with {TypeCache.GetShortName(conflictingCapability)} for IBus.");
            }

            _configurator.ReplaceScoped<IScopedBusContextProvider<IBus>, EntityFrameworkScopedBusContextProvider<IBus, TDbContext>>();
            _configurator.AddSingleton<IBusOutboxNotification, BusOutboxNotification>();

            if (_registerOutboxDeliveryService)
            {
                _configurator.AddHostedService<BusOutboxDeliveryService<TDbContext>>();
                _configurator.AddOptions<OutboxDeliveryServiceOptions>()
                    .Configure(options =>
                    {
                        options.QueryDelay = queryDelay;
                        options.QueryMessageLimit = queryMessageLimit;
                        options.QueryTimeout = queryTimeout;
                        options.MessageDeliveryLimit = messageDeliveryLimit;
                        options.MessageDeliveryTimeout = messageDeliveryTimeout;
                    });
            }
        }
    }
}
