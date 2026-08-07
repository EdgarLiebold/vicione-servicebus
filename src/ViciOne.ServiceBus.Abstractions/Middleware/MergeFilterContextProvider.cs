// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public delegate TInput MergeFilterContextProvider<TInput, in TSplit>(TInput inputContext, TSplit context)
        where TSplit : class, PipeContext
        where TInput : class, PipeContext;
}
