// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IExecuteTransformSpecification<TArguments> :
        IPipeSpecification<ExecuteContext<TArguments>>
        where TArguments : class
    {
    }
}
