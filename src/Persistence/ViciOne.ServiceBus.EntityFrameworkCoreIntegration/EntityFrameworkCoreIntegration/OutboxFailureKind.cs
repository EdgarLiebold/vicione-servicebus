namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;


public enum OutboxFailureKind
{
    None = 0,
    Transient = 1,
    Permanent = 2,
    InvariantViolation = 3,
    RetryLimitExceeded = 4,
    Unclassified = 5
}
