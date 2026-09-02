namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;


public enum OutboxDeliveryStatus
{
    Pending = 0,
    RetryScheduled = 1,
    Delivered = 2,
    Quarantined = 3
}
