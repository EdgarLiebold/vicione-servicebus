// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SagaStateMachine
{
    public interface ICompositeEventStatusAccessor<in TSaga> :
        IProbeSite
    {
        CompositeEventStatus Get(TSaga instance);

        void Set(TSaga instance, CompositeEventStatus status);
    }
}
