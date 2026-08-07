// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware.CircuitBreaker
{
    using System;
    using System.Threading.Tasks;


    public interface ICircuitBreakerBehavior :
        IProbeSite
    {
        Task PreSend();
        Task PostSend();
        Task SendFault(Exception exception);
    }
}
