// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IMissingInstanceRedeliveryConfigurator :
        IRedeliveryConfigurator
    {
        /// <summary>
        /// Use the message scheduler context instead of the redelivery context (only use when transport-level redelivery is not available)
        /// </summary>
        bool UseMessageScheduler { set; }
    }


    public interface IMissingInstanceRedeliveryConfigurator<TInstance, TData> :
        IMissingInstanceRedeliveryConfigurator
        where TInstance : SagaStateMachineInstance
        where TData : class
    {
        void OnRedeliveryLimitReached(Func<IMissingInstanceConfigurator<TInstance, TData>, IPipe<ConsumeContext<TData>>> configure);
    }
}
