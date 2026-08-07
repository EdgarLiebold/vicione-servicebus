// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IRequestStateMachineMissingInstanceConfigurator
    {
        IPipe<ConsumeContext<TMessage>> Apply<TInstance, TMessage>(IMissingInstanceConfigurator<TInstance, TMessage> configurator)
            where TInstance : SagaStateMachineInstance
            where TMessage : class;
    }
}
