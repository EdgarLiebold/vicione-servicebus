#nullable enable
namespace ViciOne.ServiceBus
{
    using System;


    public interface IEntityFrameworkBusOutboxConfigurator :
        IBusOutboxConfigurator
    {
        int MessageDeliveryLimit { set; }
        TimeSpan MessageDeliveryTimeout { get; set; }
        int MaximumDeliveryAttempts { get; set; }
        TimeSpan InitialDeliveryRetryDelay { get; set; }
        TimeSpan MaximumDeliveryRetryDelay { get; set; }

        /// <summary>
        /// Selects this DbContext as the default outbox for untyped scoped publish/send when a bus has multiple EF outboxes.
        /// DbContext-specific transactional APIs do not require a default.
        /// </summary>
        void UseAsDefault();
    }
}
