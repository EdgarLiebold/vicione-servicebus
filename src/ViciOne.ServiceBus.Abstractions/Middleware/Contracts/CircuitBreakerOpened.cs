// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts
{
    using System;


    public interface CircuitBreakerOpened
    {
        /// <summary>
        /// The exception that caused the circuit breaker to open
        /// </summary>
        Exception Exception { get; }
    }
}
