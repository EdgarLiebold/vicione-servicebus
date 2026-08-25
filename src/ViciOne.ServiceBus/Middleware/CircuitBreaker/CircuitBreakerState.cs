namespace ViciOne.ServiceBus.Middleware.CircuitBreaker
{
    using System;
    using System.Threading.Tasks;


    internal interface ICircuitBreakerBehavior :
        IProbeSite
    {
        Task PreSend();
        Task PostSend();
        Task SendFault(Exception exception);
    }


    internal readonly record struct CircuitBreakerTransition(bool Changed, Task Notification)
    {
        public static CircuitBreakerTransition Unchanged { get; } = new(false, Task.CompletedTask);
    }
}
