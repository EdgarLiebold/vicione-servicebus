// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public static class FutureResultConfiguratorExtensions
    {
        public static void SetCompleted<TResult, TInput>(this IFutureResultConfigurator<TResult, TInput> configurator)
            where TResult : class
            where TInput : class, TResult
        {
            configurator.SetCompletedUsingFactory(context => context.Message);
        }
    }
}
