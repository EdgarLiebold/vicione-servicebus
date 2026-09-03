#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Linq;
    using DependencyInjection;
    using EntityFrameworkCoreIntegration;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Middleware.Outbox;
    using Transactions;


    public class EntityFrameworkBusOutboxConfigurator<TBus, TDbContext> :
        IEntityFrameworkBusOutboxConfigurator
        where TBus : class, IBus
        where TDbContext : DbContext
    {
        readonly IBusRegistrationConfigurator _configurator;
        readonly EntityFrameworkOutboxConfigurator<TBus, TDbContext> _outboxConfigurator;
        bool _isDefault;
        bool _registerOutboxDeliveryService = true;

        public EntityFrameworkBusOutboxConfigurator(IBusRegistrationConfigurator configurator,
            EntityFrameworkOutboxConfigurator<TBus, TDbContext> outboxConfigurator)
        {
            _outboxConfigurator = outboxConfigurator;
            _configurator = configurator;
        }

        public int MessageDeliveryLimit { get; set; } = 100;
        public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public int MaximumDeliveryAttempts { get; set; } = 10;
        public TimeSpan InitialDeliveryRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaximumDeliveryRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

        public void DisableDeliveryService()
        {
            _registerOutboxDeliveryService = false;
        }

        public void UseAsDefault()
        {
            _isDefault = true;
        }

        public void Configure(Action<IEntityFrameworkBusOutboxConfigurator>? configure)
        {
            configure?.Invoke(this);
            Validate();

            EnsureCompatibleScopedContextOwner();
            EnsureUniqueRegistration();

            TimeSpan queryDelay = _outboxConfigurator.QueryDelay;
            int queryMessageLimit = _outboxConfigurator.QueryMessageLimit;
            TimeSpan queryTimeout = _outboxConfigurator.QueryTimeout;
            int messageDeliveryLimit = MessageDeliveryLimit;
            TimeSpan messageDeliveryTimeout = MessageDeliveryTimeout;
            int maximumDeliveryAttempts = MaximumDeliveryAttempts;
            TimeSpan initialDeliveryRetryDelay = InitialDeliveryRetryDelay;
            TimeSpan maximumDeliveryRetryDelay = MaximumDeliveryRetryDelay;

            _configurator.TryAddScoped<EntityFrameworkBusOutboxSessionRegistry<TBus>>();
            _configurator.AddScoped<EntityFrameworkScopedBusContext<TBus, TDbContext>>(provider =>
                provider.GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<TBus>>().GetOrCreate<TDbContext>(provider));
            _configurator.AddSingleton<IEntityFrameworkScopedBusContextFactory<TBus>>(
                new EntityFrameworkScopedBusContextFactory<TBus, TDbContext>(_isDefault));
            _configurator.ReplaceScoped<IScopedBusContextProvider<TBus>, EntityFrameworkScopedBusContextProvider<TBus>>();
            _configurator.AddScoped<IEntityFrameworkTransactionalOutbox<TBus, TDbContext>>(provider =>
                provider.GetRequiredService<EntityFrameworkScopedBusContext<TBus, TDbContext>>());
            _configurator.AddScoped<IEntityFrameworkOutboxOperations<TBus, TDbContext>, EntityFrameworkOutboxOperations<TBus, TDbContext>>();

            _configurator.AddSingleton<IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>,
                BusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>();

            _configurator.AddOptions<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>()
                .Configure(options =>
                {
                    options.QueryDelay = queryDelay;
                    options.QueryMessageLimit = queryMessageLimit;
                    options.QueryTimeout = queryTimeout;
                    options.MessageDeliveryLimit = messageDeliveryLimit;
                    options.MessageDeliveryTimeout = messageDeliveryTimeout;
                    options.MaximumDeliveryAttempts = maximumDeliveryAttempts;
                    options.InitialDeliveryRetryDelay = initialDeliveryRetryDelay;
                    options.MaximumDeliveryRetryDelay = maximumDeliveryRetryDelay;
                });

            if (_registerOutboxDeliveryService)
                _configurator.AddHostedService<BusOutboxDeliveryService<TBus, TDbContext>>();
        }

        void EnsureCompatibleScopedContextOwner()
        {
            Type serviceType = typeof(IScopedBusContextProvider<TBus>);
            Type defaultProvider = typeof(ScopedBusContextProvider<TBus>);
            Type ownProvider = typeof(EntityFrameworkScopedBusContextProvider<TBus>);

            ServiceDescriptor? conflict = _configurator.FirstOrDefault(descriptor =>
                descriptor.ServiceType == serviceType
                && descriptor.ImplementationType != defaultProvider
                && descriptor.ImplementationType != ownProvider);

            if (conflict == null)
                return;

            string owner = conflict.ImplementationType?.Name ?? conflict.ServiceType.Name;
            throw new ConfigurationException(
                $"The Entity Framework bus outbox cannot replace scoped context owner {owner} for {TypeCache<TBus>.ShortName}.");
        }

        void EnsureUniqueRegistration()
        {
            Type marker = typeof(EntityFrameworkBusOutboxRegistration<TBus, TDbContext>);
            if (_configurator.Any(x => x.ServiceType == marker))
                throw new ConfigurationException(
                    $"The Entity Framework bus outbox for {TypeCache<TBus>.ShortName} and {TypeCache<TDbContext>.ShortName} is already configured.");

            if (_isDefault && _configurator.Any(x =>
                    x.ServiceType == typeof(IEntityFrameworkScopedBusContextFactory<TBus>)
                    && x.ImplementationInstance is IEntityFrameworkScopedBusContextFactory<TBus> { IsDefault: true }))
            {
                throw new ConfigurationException(
                    $"A default Entity Framework bus outbox is already configured for {TypeCache<TBus>.ShortName}. Exactly one default is allowed.");
            }

            _configurator.AddSingleton(new EntityFrameworkBusOutboxRegistration<TBus, TDbContext>());
        }

        void Validate()
        {
            if (MessageDeliveryLimit <= 0)
                throw new ConfigurationException("MessageDeliveryLimit must be greater than zero.");
            if (MessageDeliveryTimeout <= TimeSpan.Zero)
                throw new ConfigurationException("MessageDeliveryTimeout must be greater than zero.");
            if (MaximumDeliveryAttempts <= 0)
                throw new ConfigurationException("MaximumDeliveryAttempts must be greater than zero.");
            if (InitialDeliveryRetryDelay <= TimeSpan.Zero)
                throw new ConfigurationException("InitialDeliveryRetryDelay must be greater than zero.");
            if (MaximumDeliveryRetryDelay < InitialDeliveryRetryDelay)
                throw new ConfigurationException("MaximumDeliveryRetryDelay must be greater than or equal to InitialDeliveryRetryDelay.");
        }
    }
}
