// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Middleware
{
    public interface CommandRetryContext
    {
        /// <summary>
        /// The retry attempt in progress, or zero if this is the first time through
        /// </summary>
        int RetryAttempt { get; }
    }
}
